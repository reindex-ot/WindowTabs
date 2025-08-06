namespace Bemo

type ManagerViewService() =
    let mutable currentForm : DesktopManagerForm option = None
    
    interface IManagerView with
        member x.show() =
            currentForm <- Some(new DesktopManagerForm())
            currentForm.Value.show()

        member x.show(view) =
            currentForm <- Some(new DesktopManagerForm())
            currentForm.Value.showView(view)
            
    member x.closeCurrentForm() =
        match currentForm with
        | Some form -> 
            try
                form.close()
            with
            | _ -> ()
            currentForm <- None
        | None -> ()