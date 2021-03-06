namespace WebSharper.UI.Next.Tests

open WebSharper
open WebSharper.JavaScript

open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Notation
open WebSharper.UI.Next.Templating

[<JavaScript>]
module Client =    
    open WebSharper.UI.Next.Client

    let [<Literal>] TemplateHtmlPath = __SOURCE_DIRECTORY__ + "/template.html"

    type MyTemplate = Template<TemplateHtmlPath> 

    type Item =
        { id : int; name: string; description: string }
        static member Key x = x.id

#if ZAFIR
    [<SPAEntryPoint>]
    let Main() =
#else
    let Main =
#endif
        let myItems =
            ListModel.CreateWithStorage Item.Key (Storage.LocalStorage "Test" Serializer.Default)

        let newName = Var.Create ""
        let newDescr = Var.Create ""
        let itemsSub = Submitter.Create myItems.View Seq.empty
        let stitle = "Starting titlo"
        let var = Var.Create ""

        let title = 
            stitle
            |> Seq.toList
            |> List.map Var.Create

        async {
            do! Async.Sleep 1500
            Var.Set (List.nth title (title.Length - 1)) 'e'
        } |> Async.Start

        let tv = title
                 |> Seq.map View.FromVar
                 |> View.Sequence
                 |> View.Map (fun e -> new string(Seq.toArray e))
        let btnSub = Submitter.Create var.View ""
 
        let mutable lastKey = myItems.Length
        let freshKey() =
            lastKey <- lastKey + 1
            lastKey

        let findByKey = Var.Create ""
        let found = 
            findByKey.View.BindInner(fun s -> 
                myItems.TryFindByKeyAsView(int s).Map(function 
                    | None -> "none" 
                    | Some a -> a.name + ":" + a.description))

        let doc =
            MyTemplate.Doc(
                NewName = newName,
                NewDescription = newDescr,
                NewItem = (fun e v -> myItems.Add { id = freshKey(); name = newName.Value; description = newDescr.Value }),
                Title = [
                    h1Attr [
                        attr.style "color: blue"
                        attr.classDynPred var.View (View.Const true)
                        on.click (fun el ev -> Console.Log ev)
                    ] [textView tv]
                ],
                ListContainer = [
                    myItems.ViewState.DocSeqCached(Item.Key, fun key item ->
                        MyTemplate.ListItem.Doc(
                            Key = item.Map(fun i -> string i.id),
                            Name = item.Map(fun i -> i.name),
                            Description = myItems.LensInto (fun i -> i.description) (fun i d -> { i with description = d }) key,
                            FontStyle = "italic",
                            FontWeight = "bold",
                            Remove = (fun _ _ -> myItems.RemoveByKey key))
                    )
                ],
                SubmitItems = (fun el ev -> itemsSub.Trigger ()),
                ClearItems = (fun el ev -> myItems.Clear ()),
                FindBy = findByKey,
                Found = found,
                Length = myItems.ViewState.Map(fun s -> printfn "mapping length"; string s.Length),
                Names = 
                    myItems.ViewState.Map(fun s -> 
                        s.ToArray(fun i -> not (System.String.IsNullOrEmpty i.description))
                        |> Seq.map (fun i -> i.name)
                        |> String.concat ", "
                    ),
                ListView = [
                    itemsSub.View.DocSeqCached(Item.Key, fun key item ->
                        MyTemplate.ListViewItem.Doc(
                            Name = item.Map(fun i -> i.name),
                            Description = item.Map(fun i -> i.description)
                        )
                    )
                ],
                MyInput = var,
                MyInputView = btnSub.View,
                MyCallback = (fun el ev -> btnSub.Trigger ()),
                NameChanged = (fun el ev -> 
                    let key = if ev?which then ev?which else ev?keyCode
                    if key = 13 then newName := ""),
                PRendered = (fun el -> var := el.GetAttribute("id"))
            )

        Anim.UseAnimations <- false

        div [
            doc 
            Regression67.Doc
        ]
        |> Doc.RunById "main"

        Console.Log("Running JavaScript Entry Point..")
