#load "ScrapeM-refs.fsx"
#load "src\ScrapeM\ScrapeM.fs"

open System.Collections.Generic
open FSharpPlus
open FSharpPlus.Data
open FSharpPlus.Operators.Arrows
open ScrapeM

let request a b: StateT<_,_ seq> = hoistState (ScrapeM.request a b) // Use stateful sequences

type News = {User : string; Title : string; IntroText : string}

let q = monad.plus.strict {
    for credential in [("stopthebug", "stopthebug1"); ("aslzzztk", "abcd1234")] do
    for data       in [[KeyValuePair ("username", fst credential); KeyValuePair ("password", snd credential)]] do
    let!  _  = request "https://secure.moddb.com/members/login" None
    let!  _  = request "" (Some data)
    let! htm = request ("http://www.moddb.com/members/" + fst credential + "/comments") None
    for lnk in htm |> parse |> cssSelect "a[class='related'] a[href^='/news']" |>> (attributes >> item "href" &&& innerText) |> distinct do
    let! doc = (request ("http://www.moddb.com" + fst lnk) None) |>> parse
    select {
        User      = fst credential
        Title     = snd lnk
        IntroText = cssSelect "p[class='introductiontext']" doc |> head |> innerText } into news
    where (length news.IntroText < 150)
    select news}

let news = StateT.run q Context.Empty |> Seq.map fst
printTable news