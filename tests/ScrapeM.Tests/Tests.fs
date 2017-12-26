module ScrapeM.Tests

open FSharp.Data
open FSharpPlus
open FSharpPlus.Data
open ScrapeM
open NUnit.Framework

[<Test>]
let ``This project has reached at least two milestones`` () =
    let request a b = (ScrapeM.request a b |> State.eval) Context.Empty
    let r = 
        request "https://github.com/gusty/ScrapeM/milestones?state=closed" None 
        |> parse
        |> cssSelect "a[href*=/milestone/]" 
        |>> fun x -> x.InnerText()        
    printfn "%A" r
    Assert.GreaterOrEqual(length r, 2)