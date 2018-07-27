open System
open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

open Giraffe
open Shared

open Fable.Remoting.Server
open Fable.Remoting.Giraffe

open StackExchange.Profiling

let publicPath = Path.GetFullPath "../Client/public"
let port = 8085us

let getInitCounter () : Task<Counter> = task { return 42 }

let ping () = async {
                // How to profile code: https://miniprofiler.com/dotnet/HowTo/ProfileCode

                // something sync
                using (MiniProfiler.Current.Step("Let's pretend we calculate something...")) (fun _ ->
                    System.Threading.Thread.Sleep 123
                )

                // something async
                use _mp = MiniProfiler.Current.Step("Let's call a db asynchronously...")
                do! Async.Sleep 42

                return "pong (from remoting)"
              }

let counterApi = {
    initialCounter = getInitCounter >> Async.AwaitTask
    ping = ping
}

let webApp =
    choose [
        GET >=> choose [
            route "/"       >=> (fun next ctx -> htmlView (Views.index ctx) next ctx)
            // do nothing special, and the overall time of the request is logged
            route "/ping"   >=> json "pong"
        ]

        Remoting.createApi()
        |> Remoting.withRouteBuilder Route.builder
        |> Remoting.fromValue counterApi
        |> Remoting.buildHttpHandler
    ]


let configureApp (app : IApplicationBuilder) =
    app.UseDefaultFiles()
       .UseStaticFiles()
       .UseMiniProfiler()
       .UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore

    // IMemoryCache is required for MiniProfiler's in-memory storage
    services.AddMemoryCache()
            .AddMiniProfiler(fun mp ->
                // Config options for ASP.NET Core: https://miniprofiler.com/dotnet/AspDotNetCore
                mp.ShowControls <- true
                mp.PopupRenderPosition <- RenderPosition.Right
                ()) |> ignore

WebHost
    .CreateDefaultBuilder()
    .UseWebRoot(publicPath)
    .UseContentRoot(publicPath)
    .Configure(Action<IApplicationBuilder> configureApp)
    .ConfigureServices(configureServices)
    .UseUrls("http://0.0.0.0:" + port.ToString() + "/")
    .Build()
    .Run()