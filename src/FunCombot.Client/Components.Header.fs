namespace FunCombot.Client.Components.Header

open Bolero
open Bolero.Html
open FunCombot.Client
open FunCombot.Client.Types

module HeaderComponent =   
    type HeaderTemplate = Template<"""frontend/templates/header.html""">
        
    type HeaderComponentMessage =
        | ChangeChat of Chat
    
    type HeaderComponentModel = {
        CurrentChat: Chat
    }
    
    let headerTemplate = HeaderTemplate()
    
    let update message model =
        match message with
        | ChangeChat chat ->
            { model with CurrentChat = chat }
            
    type HeaderComponent() =
        inherit ElmishComponent<HeaderComponentModel, HeaderComponentMessage>()
        
        override this.View model dispatch =
            let dropDown =
                forEach getUnionCases<Chat> ^fun (case, name, _) ->
                    a [ attr.classes [yield "item"; if model.CurrentChat = case then yield "active selected"];
                        on.click ^fun ev ->
                            if model.CurrentChat <> case then dispatch (ChangeChat case)] [
                        text case.DisplayName
                    ]
            headerTemplate
                .HeaderItem(text "StatsBot")
                .DropdownItems(dropDown)
                .ChatName(text model.CurrentChat.DisplayName)
                .Elt()
