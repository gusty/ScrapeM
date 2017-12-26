#load "ScrapeM.fsx"
#load "src\ScrapeM\ScrapeM.fs"

open System.Collections.Generic
open FSharpPlus
open FSharpPlus.Data
open ScrapeM

// Simple Stateful query with a single result

type News = {User : string; Title : string; IntroText : string}

let q = monad.plus {
    let! credential = result ("stopthebug", "stopthebug1")
    let!  _  = request "https://secure.moddb.com/members/login" None
    let!  _  = request "" (Some [KeyValuePair ("username", fst credential); KeyValuePair ("password", snd credential)])
    let! htm = request ("http://www.moddb.com/members/" + fst credential) None
    return htm |> parse |> cssSelect "p" |> head |> innerText }

let bio = State.eval q Context.Empty