namespace MiniBlog

open WebSharper
open WebSharper.JavaScript
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.UI.Templating
open WebSharper.UI.Notation

open Routing

[<JavaScript>]
module Templates =

    type MainTemplate = Templating.Template<"Main.html", ClientLoad.FromDocument, ServerLoad.WhenChanged>

[<JavaScript>]
module Client =

    // Handles Home and Article pages
    let ReadPage endpoint =
        let endpointVar = Var.Create endpoint
        
        siteRouter
        |> Router.Filter (
            function
            | Routing.ReadPage -> true
            | _ -> false
        )
        |> Router.InstallInto endpointVar Home

        let createContent (t: string) = 
            t.Split('\n')
            |> Seq.map (fun l -> p [] [ text l ])
            |> Doc.Concat

        endpointVar.View
        |> Doc.BindView (
            function
            | Home ->
                async {
                    let! articles = Server.GetArticles()
                    return
                        articles 
                        |> Seq.map (fun a ->
                            Templates.MainTemplate.Article()
                                .Title(a.Title)
                                .Text(createContent a.Text)
                                .ReadArticleLink(siteRouter.Link (Article a.Id))
                                .EditArticleLink(siteRouter.Link (Edit a.Id))
                                .Doc()
                        )
                        |> Doc.Concat
                }
                |> Doc.Async
            | Article i ->
                async {
                    let! a = Server.GetArticle i
                    return
                        Templates.MainTemplate.FullArticle()
                            .Title(a.Title)
                            .Text(createContent a.Text)
                            .HomeLink(siteRouter.Link Home)
                            .EditArticleLink(siteRouter.Link (Edit a.Id))
                            .Doc()
                }
                |> Doc.Async
            | _ ->
                Doc.Empty
        )

    let redirectTo endpoint =
        JS.Window.Location.Href <- siteRouter.Link endpoint

    let EditPage endpoint =
        let endpointVar = Var.Create endpoint
        
        siteRouter
        |> Router.Filter (
            function
            | Edit _ -> true
            | _ -> false
        )
        |> Router.InstallInto endpointVar Home

        let articleTitle = Var.Create ""

        endpointVar.View
        |> Doc.BindView (
            function
            | Edit articleId ->
                async {
                    let! article = 
                        if articleId = 0 then
                            {
                                Id = 0
                                Title = ""
                                Text = ""
                            }
                            |> async.Return
                        else
                            Server.GetArticle articleId
                    articleTitle.Value <- article.Title
                    
                    let submit isSave =
                        let textArea = JS.Document.GetElementById("Text") :?> HTMLTextAreaElement
                        let article =
                            {
                                Id = articleId
                                Title = articleTitle.Value
                                Text = textArea.Value
                            }
                        async {
                            let! newId = Server.SubmitArticle article
                            if isSave then
                                if articleId <> newId then
                                    redirectTo (Edit newId)
                            else
                                redirectTo (Article newId)
                        }
                        |> Async.StartImmediate

                    return 
                        Templates.MainTemplate.EditArticle()
                            .OnDelete(fun _ -> 
                                async {
                                    if articleId <> 0 then
                                        do! Server.DeleteArticle articleId
                                        redirectTo Home
                                }
                                |> Async.StartImmediate
                            )
                            .OnSave(fun _ -> submit true)
                            .OnSubmit(fun _ -> submit false)
                            .Title(articleTitle)
                            .Text(article.Text)
                            .Doc()
                }
                |> Doc.Async    
            | _ ->
                Doc.Empty
        )
