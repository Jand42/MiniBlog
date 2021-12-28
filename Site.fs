namespace MiniBlog

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Server

open Routing

module Templating =
    open WebSharper.UI.Html

    // Compute a menubar where the menu item for the given endpoint is active
    let MenuBar (ctx: Context<EndPoint>) endpoint : Doc list =
        let ( => ) txt act =
             li [if endpoint = act then yield attr.``class`` "active"] [
                a [attr.href (ctx.Link act)] [text txt]
             ]
        [
            "Home" => EndPoint.Home
            "New Blog Article" => EndPoint.Edit 0
        ]

    let Main ctx action (title: string) (body: Doc list) =
        Content.Page(
            Templates.MainTemplate()
                .Title(title)
                .MenuBar(MenuBar ctx action)
                .Body(body)
                .Doc()
        )

module Site =
    open WebSharper.UI.Html

    [<Website>]
    let Main =
        Sitelet.New siteRouter (fun ctx endpoint ->
            match endpoint with
            | ReadPage -> 
                Templating.Main ctx endpoint "MiniBlog" [
                    div [] [client <@ Client.ReadPage endpoint @>]
                ]
            | EditPage -> 
                Templating.Main ctx endpoint "Edit article" [
                    div [] [client <@ Client.EditPage endpoint @>]
                ]
        )
