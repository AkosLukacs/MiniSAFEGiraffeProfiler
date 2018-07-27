module Views

open Giraffe
open GiraffeViewEngine
open Microsoft.AspNetCore.Http
open StackExchange.Profiling

let index (ctx: HttpContext) =
        let mp = MiniProfiler.Current.RenderIncludes(ctx)
        html [] [
            head [] [
                meta [ _httpEquiv "Content-Type"; _content "text/html"; _charset "utf-8" ]
                title [ ]  [ encodedText "SAFE demo with MiniProfiler for .NET" ]

                meta [ _name "viewport"; _content "width=device-width, initial-scale=1"]

                link [ _rel  "stylesheet"; _type "text/css"; _href "https://cdnjs.cloudflare.com/ajax/libs/bulma/0.6.1/css/bulma.min.css" ]
                link [ _rel  "stylesheet"; _type "text/css"; _href "https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.min.css" ]
                link [ _rel  "stylesheet"; _type "text/css"; _href "https://fonts.googleapis.com/css?family=Open+Sans" ]

                link [ _rel "shortcut icon"; _href "/Images/safe_favicon.png"; _type "image/png" ]
            ]
            body [ ] [
                div [ _id "elmish-app" ] []

                // Include MiniProfiler script before your main js bundle, because MiniProfiler 'patches' XHR, fetch, etc to profile those requests...
                rawText mp.Value

                script [ _src "./js/bundle.js" ] [ ]
            ]
        ]
