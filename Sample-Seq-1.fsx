#load "ScrapeM.fsx"
open System
open System.Text.RegularExpressions
open FSharp.Data
open FSharpPlus
open ScrapeM

let request a b: seq<_> = (ScrapeM.request a b |> State.eval) Context.Empty |> result // Use (non-stateful) sequences

type Cme = FSharp.Data.HtmlProvider<"http://www.cmegroup.com/trading/interest-rates/stir/eurodollar.html">
type Row = Cme.QuotesFuturesProductTable1.Row
type InterestRate<'a, 'b, 'c, 'd, 'e> = {Item : 'a; Year : 'b; Month: 'c; Low : 'd; High : 'd; Mid : 'e}

let q = linq {
    let! html = request "http://www.cmegroup.com" None 
    for lnk in html |> parse |> cssSelect "td>a a[href^='/trading/interest-rates/'] a[href$='html']" |>> (fun x -> x.AttributeValue "href") do
    for row in Cme.Load(@"http://www.cmegroup.com" + lnk).Tables.QuotesFuturesProductTable1.Rows |> skip 1 do
    select {
        Item  = (Regex("/(([a-zA-Z]|-|[0-9])*).html").Match(lnk).Groups |> toSeq |> skip 1 |> Seq.head).Value
        Year  = 0
        Month = parse ((row:Row).Month)
        Low   = String.replace "'" "" row.Low
        High  = String.replace "'" "" row.High
        Mid   = "" } into row
    where (length row.Low > 1)
    select {
        Item  = row.Item
        Year  = (row.Month:DateTime).Year
        Month = (row.Month:DateTime).Month        
        Low   = (tryParse row.Low  : float option)
        High  = (tryParse row.High : float option)
        Mid   = ((*) 0.5) <!> ((+) <!> (tryParse row.Low: float option) <*> tryParse row.High)} 
    }

let table = q |> toList
printTable table

(* will print something like:
.------------------------------------------------------------------------------------.
|Item                          |Year|Month|Low          |High         |Mid           |
|------------------------------------------------------------------------------------|
|eurodollar                    |2016|12   |Some(99.0075)|Some(99.0175)|Some(99.0125) |
|eurodollar                    |2017|1    |Some(99)     |Some(99.01)  |Some(99.005)  |
|eurodollar                    |2017|2    |             |             |              |
|eurodollar                    |2017|3    |Some(98.955) |Some(98.975) |Some(98.965)  |
|eurodollar                    |2026|9    |             |             |              |
|2-year-us-treasury-note       |2016|12   |Some(108222) |Some(108237) |Some(108229.5)|
|2-year-us-treasury-note       |2017|3    |Some(108132) |Some(108162) |Some(108147)  |
|10-year-us-treasury-note      |2017|9    |             |             |              |
|ultra-10-year-us-treasury-note|2016|12   |Some(135060) |Some(135110) |Some(135085)  |
|ultra-10-year-us-treasury-note|2017|3    |Some(134110) |Some(134295) |Some(134202.5)|
|30-year-us-treasury-bond      |2017|3    |Some(15021)  |Some(15119)  |Some(15070)   |
|30-year-us-treasury-bond      |2017|6    |             |             |              |
|30-year-us-treasury-bond      |2017|9    |             |             |              |
|ultra-t-bond                  |2016|12   |Some(16123)  |Some(16203)  |Some(16163)   |
|ultra-t-bond                  |2017|3    |Some(16012)  |Some(16122)  |Some(16067)   |
|ultra-t-bond                  |2017|6    |             |             |              |
|ultra-t-bond                  |2017|9    |             |             |              |
...
*)