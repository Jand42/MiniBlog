namespace MiniBlog

open WebSharper
open WebSharper.Web
open System
open System.IO
open System.Collections.Generic

[<JavaScript>]
type Article = 
    {
        Id: int
        Title: string
        Text: string
    }

module Server =
    
    let articlesCache = Dictionary<int, Article>()

    let getArticlesFolder root =
        let articlesFolder = Path.Combine(root, "articles")
        if not (Directory.Exists(articlesFolder)) then
            Directory.CreateDirectory(articlesFolder) |> ignore
        articlesFolder

    let initArticles =
        lazy
            let ctx = Remoting.GetContext()
            let articlesFolder = getArticlesFolder ctx.RootFolder
            for f in Directory.GetFiles(articlesFolder) do
                let lines = File.ReadAllLines f
                let article =
                    {
                        Id = Path.GetFileNameWithoutExtension f |> int
                        Title = lines.[0]
                        Text = lines.[1 ..] |> String.concat Environment.NewLine
                    }
                articlesCache.Add(article.Id, article)

    [<Rpc>]
    let GetArticles () =
        initArticles.Value
        articlesCache.Values |> Array.ofSeq |> async.Return
        
    [<Rpc>]
    let GetArticle i =
        initArticles.Value
        articlesCache.[i] |> async.Return

    [<Rpc>]
    let SubmitArticle (a: Article) =
        let ctx = Remoting.GetContext()
        let articlesFolder = getArticlesFolder ctx.RootFolder
        let a =
            if a.Id = 0 then 
                let newId =
                    if articlesCache.Count = 0 then
                        1
                    else
                        (articlesCache.Keys |> Seq.max) + 1

                { a with Id = newId }
            else
                a
        articlesCache.[a.Id] <- a
        async {
            let f = Path.Combine(articlesFolder, string a.Id + ".txt")
            File.WriteAllLines(f, [| a.Title; a.Text |])
            return a.Id
        }

    [<Rpc>]
    let DeleteArticle i =
        let ctx = Remoting.GetContext()
        let articlesFolder = getArticlesFolder ctx.RootFolder
        if articlesCache.ContainsKey(i) then
            articlesCache.Remove(i) |> ignore
            let f = Path.Combine(articlesFolder, string i + ".txt")
            File.Delete(f)
        async.Return ()


