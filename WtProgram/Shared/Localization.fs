namespace Bemo
open System

module Localization =
    type Language =
        | English
        | Japanese

    let mutable private currentLanguage = English

    let languageChanged = Event<unit>()

    let getCurrentLanguage() =
        currentLanguage

    let setLanguage(lang: Language) =
        if currentLanguage <> lang then
            currentLanguage <- lang
            languageChanged.Trigger()

    let setLanguageByString(langStr: string) =
        match langStr with
        | "ja" -> setLanguage(Japanese)
        | "en" -> setLanguage(English)
        | _ -> setLanguage(English)

    let getLanguageString() =
        match currentLanguage with
        | English -> "en"
        | Japanese -> "ja"

    let getString(key: string) =
        let strings =
            match currentLanguage with
            | English -> Localization_en.strings
            | Japanese -> Localization_ja.strings

        match strings.TryGetValue(key) with
        | true, value -> value
        | false, _ ->
            // Fallback to English if key not found in current language
            match Localization_en.strings.TryGetValue(key) with
            | true, value -> value
            | false, _ -> key  // Last resort: return the key itself
