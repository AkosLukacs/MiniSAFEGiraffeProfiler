module Client

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack.Fetch

open Shared

open Fulma
open Fable.PowerPack
open Fable.Import


// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = { Counter: Counter option }

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
| Increment
| Decrement
| InitialCountLoaded of Result<Counter, exn>
| PingFetch
| PingRemoting
| GotPing of Result<string, exn>


module Server =

    open Shared
    open Fable.Remoting.Client

    /// A proxy you can use to talk to server directly
    let api : ICounterApi =
      Remoting.createApi()
      |> Remoting.withRouteBuilder Route.builder
      |> Remoting.buildProxy<ICounterApi>()


// defines the initial state and initial command (= side-effect) of the application
let init () : Model * Cmd<Msg> =
    let initialModel = { Counter = None }
    let loadCountCmd =
        Cmd.ofAsync
            Server.api.initialCounter
            ()
            (Ok >> InitialCountLoaded)
            (Error >> InitialCountLoaded)
    initialModel, loadCountCmd

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match currentModel.Counter, msg with
    | Some x, Increment ->
        let nextModel = { currentModel with Counter = Some (x + 1) }
        nextModel, Cmd.none
    | Some x, Decrement ->
        let nextModel = { currentModel with Counter = Some (x - 1) }
        nextModel, Cmd.none
    | _, InitialCountLoaded (Ok initialCount)->
        let nextModel = { Counter = Some initialCount }
        nextModel, Cmd.none

    | _, PingFetch ->
        let fetchPing () =
            fetch "/ping" []
            |> Promise.bind (fun res -> res.text())
        let cmd = Cmd.ofPromise
                        fetchPing
                        ()
                        (Ok >> GotPing)
                        (Error >> GotPing)
        currentModel, cmd

    | _, PingRemoting ->
        let cmd = Cmd.ofAsync
                        Server.api.ping
                        ()
                        (Ok >> GotPing)
                        (Error >> GotPing)
        currentModel, cmd

    | _, GotPing p ->
        match p with
        | Ok pingResp ->
            Browser.console.log("Ping ok:", pingResp)
        | Error err ->
            Browser.console.error("Ping error:", err)
        currentModel, Cmd.none

    | _ -> currentModel, Cmd.none


let safeComponents =
    let intersperse sep ls =
        List.foldBack (fun x -> function
            | [] -> [x]
            | xs -> x::sep::xs) ls []

    let components =
        [
            "Giraffe", "https://github.com/giraffe-fsharp/Giraffe"
            "Fable", "http://fable.io"
            "Elmish", "https://elmish.github.io/elmish/"
            "Fulma", "https://mangelmaxime.github.io/Fulma"
            "Fable.Remoting", "https://zaid-ajaj.github.io/Fable.Remoting/"
            "MiniProfiler for .NET", "https://miniprofiler.com/dotnet/"
        ]
        |> List.map (fun (desc,link) -> a [ Href link ] [ str desc ] )
        |> intersperse (str ", ")
        |> span [ ]

    p [ ]
        [ strong [] [ str "SAFE Template" ]
          str " powered by: "
          components ]

let show = function
| { Counter = Some x } -> string x
| { Counter = None   } -> "Loading..."

let button txt onClick =
    Button.button
        [ Button.IsFullWidth
          Button.Color IsPrimary
          Button.OnClick onClick ]
        [ str txt ]

let view (model : Model) (dispatch : Msg -> unit) =
    div []
        [ Navbar.navbar [ Navbar.Color IsPrimary ]
            [ Navbar.Item.div [ ]
                [ Heading.h2 [ ]
                    [ str "SAFE demo with "
                      a [ Href "https://miniprofiler.com/dotnet" ] [ str "MiniProfiler for .NET"] ] ] ]

          Container.container []
              [ Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                    [ Heading.h3 [] [ str ("Press buttons to manipulate counter: " + show model) ]  ]
                Columns.columns []
                    [ Column.column [] [ button "-" (fun _ -> dispatch Decrement) ]
                      Column.column [] [ button "+" (fun _ -> dispatch Increment) ] ]

                Card.card [] [
                    Card.header [] [ Heading.h3 [ ] [ str "Miniprofiler-demo" ] ]
                    Card.content [] [
                        Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                            [ Heading.h3 [] [ str "Press 'Ping!' buttons to call the server (and watch miniprofiler timings)" ] ]
                        Columns.columns []
                            [ Column.column [] [ button "Ping! (fetch)" (fun _ -> dispatch PingFetch) ]
                              Column.column [] [ button "Ping! (remoting)" (fun _ -> dispatch PingRemoting) ] ]
                        Content.content []
                            [ str "You can see the list or results at "
                              a [ Href "/mini-profiler-resources/results-index"] [ str "/mini-profiler-resources/results-index" ] ]
                    ]
                ]
              ]

          Footer.footer [ ]
                [ Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                    [ safeComponents ] ] ]


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
