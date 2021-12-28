namespace MiniBlog

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Server

[<JavaScript>]
module Routing =

    type EndPoint =
        | [<EndPoint "/">] Home
        | [<EndPoint "/article">] Article of id: int
        | [<EndPoint "/edit">] Edit of id: int

    let (|ReadPage|EditPage|) e =
        match e with
        | Home
        | Article _ -> ReadPage
        | Edit _ -> EditPage

    let siteRouter = Router.Infer<EndPoint>()
