# ScrapeM
A monadic web scraping library

This library makes [web scraping](https://en.wikipedia.org/wiki/Web_scraping) easier by providing ways to automatically maintain state through different request, handling cookies, form submission and http headers.

*One function to scrap'em all*

This is essentially a single-function library which integrates many existing libraries and present several ways to approach web scraping by using different monads.

All other common functions used here come from different libraries like [FSharp.Data](http://fsharp.github.io/FSharp.Data/), [Http.fs](https://github.com/haf/Http.fs) and [F#+](https://github.com/gmpl/FSharpPlus)

*Scrapes the web with category*

It's possible to create stateful linq-style queries which simulates basic user interaction with form submission by using different flavours of State monads. Also sequences expressions are available to integrate the data being extracted from multiple webpages in the same query.

##Getting started

In order to try the examples run:

    > build.cmd // on windows    
    $ ./build.sh  // on unix
    
Now you can try the sample files:


* [Basic query with state handling](Sample-State-1.fsx) - Extracts a text from a website with login.
* [Basic query with multiple results](Sample-Seq-1.fsx) - Extracts many texts from a website.
* [Advanced query with state handling and multiple results](Sample-StateT-Seq-1.fsx) - Extracts many texts from a website by using different logins.
