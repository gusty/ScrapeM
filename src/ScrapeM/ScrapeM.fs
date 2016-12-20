module ScrapeM

open Hopac
open HttpFs.Client
open System
open System.IO
open System.Text
open System.Collections.Generic
open System.Web

open FSharp.Data
open FSharpPlus

let printTable x =
    let lines (lst: 'Record seq) = 
        let fields = Reflection.FSharpType.GetRecordFields typeof<'Record>
        let headers = fields |> Seq.map (fun field -> field.Name)
        let asList (x:'record) = fields |> Seq.map (fun field -> string (Reflection.FSharpValue.GetRecordField(x, field)))
        let rows = Seq.map asList lst
        let table = seq {yield headers; yield! rows}
        let maxs = table |> (traverse ZipList >> ZipList.run) |>> map length |>> maxBy id
        let rowSep = String.replicate (sum maxs + length maxs - 1) "-"
        let fill (i, s) = s + String.replicate (i - length s) " "
        let printRow r = "|" + (r |> zip maxs |>> fill |> intercalate "|") + "|"
        seq {
            yield "." + rowSep + "."
            yield printRow headers
            yield "|" + rowSep + "|"
            yield! (rows |>> printRow)
            yield "'" + rowSep + "'" }
    x |> Seq.toList |> lines|> iter (printfn "%s")

let cssSelect s (x:HtmlDocument) = x.CssSelect s
let attributes (x:HtmlNode) = x.Attributes() |> List.map (fun x -> x.Name(), x.Value()) |> Map.ofSeq
let innerText  (x:HtmlNode) = x.InnerText()


let private paramsToBody (acc:string) (tArg:KeyValuePair<string,string>) =     
    let rName, rValue  = (tArg.Key, tArg.Value) |> join bimap HttpUtility.UrlEncode // Encode characters like spaces before transmitting.
    let mix = HttpUtility.UrlEncode rName + "=" + rValue
    match acc with
    | "" -> mix // Skip first.
    | _  -> acc + "&" + mix  // application/x-www-form-urlencoded


type Context = {Url : string option; Html : string option; Cookies : Map<string, string>} with
    static member Empty = {Url = None; Html = None; Cookies = getEmpty()}

let request (url:string) postData =
    let rec loop rqUrl postData = State (fun (state:Context) ->
        let url = (if rqUrl = "" then state.Url.Value else rqUrl)
        #if RELEASE
        #else
        printfn "Request: %A  cookies: %A" url state.Cookies
        #endif

        let request = 
            Request.createUrl (match postData with None -> Get | _ -> Post) url // todo get it from the form
            |> Request.autoFollowRedirectsDisabled
            |> match state.Url with
                | None   -> id
                | Some x -> Request.setHeader (Referer x)            
            |> match postData with
                | None        -> id
                | Some values ->
                    let currentValues =
                        match state.Html with
                        | None -> []
                        | Some txt ->
                            let html = parse txt
                            let hidden_inputs = cssSelect "input[type=hidden]" html
                            let img_inputs    = cssSelect "input[type=image]"  html
                            hidden_inputs 
                                    |>> (fun x -> KeyValuePair((x.Attribute "name").Value(), (x.Attribute "value").Value()))
                                    <|> ([".x"; ".y"] |>> fun a -> KeyValuePair((img_inputs.[0].Attribute "name").Value() + a, "1"))
                    
                    Request.bodyString (fold paramsToBody "" (currentValues ++ values)) >> Request.setHeader (ContentType (ContentType.parse "application/x-www-form-urlencoded").Value)
        
        let requestWithCookiesOfPrevResponse = Map.fold (fun rq k v -> Request.cookie (Cookie.create (k, v)) rq) request state.Cookies
        let response = requestWithCookiesOfPrevResponse |> getResponse |> Alt.toAsync |> Async.RunSynchronously
        
        let html = (new StreamReader(response.body)).ReadToEnd()
       
        if response.statusCode = 302 then
            let redir = (html |> parse |> cssSelect "a").Head.Attributes().Head.Value()
            let redirUrl = (if redir = "/" then url else redir)
            #if RELEASE
            #else
            printfn "Redirect to: %A" redirUrl
            #endif
            State.run (loop redirUrl None) {Url = Some url; Html = state.Html; Cookies = state.Cookies ++ ofSeq response.cookies}
        else
            html, {Url = Some url; Html = Some html; Cookies = state.Cookies ++ ofSeq response.cookies})
    loop url postData

let inline hoistState x = (StateT << (fun a -> result << a) << State.run) x