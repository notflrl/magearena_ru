using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Recognissimo.Components;
using System.Collections.Generic;
using UnityEngine;

namespace MageArena_Ru
{
    [BepInPlugin("org.floral.magearenaru", "Mage Arena - Ru", "1.0.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        bool openMenu = false;
        string lastRecognized = "";
        int guioffset = 0;

        // global
        string ruVoiceModel = "LanguageModels/vosk-model-small-ru-0.22";

        // keycodes
        KeyCode resetRussian             = KeyCode.F1;
        KeyCode menuOpen                 = KeyCode.F2;

        // default
        string[] fireball_command        = { "шар" };            /* original: ball */
        string[] frostbolt_command       = { "холод" };          /* original: freeze */
        string[] worm_command            = { "вход" };           /* original: worm */
        string[] hole_command            = { "выход" };          /* original: hole */
        string[] magicmissle_command     = { "атака" };          /* original: magic */

        // ?
        string[] mirror_command          = { "зеркало" };        /* original: mirror */

        // unlockable
        string[] poofspell_command       = { "blink" };          /* original: blink */
        string[] thunderbolt_command     = { "thunderbolt" };    /* original: thunderbolt */
        string[] blast_command           = { "два" };            /* original: dark blast */
        string[] holylight_command       = { "divine" };         /* original: divine */
        string[] wisp_command            = { "wisp" };           /* original: wisp */

        // config
        private ConfigEntry<string> config_voiceModel;
        private ConfigEntry<string> config_fireball_command;
        private ConfigEntry<string> config_frostbolt_command;
        private ConfigEntry<string> config_worm_command;
        private ConfigEntry<string> config_hole_command;
        private ConfigEntry<string> config_magicmissle_command;
        private ConfigEntry<string> config_mirror_command;
        private ConfigEntry<string> config_poofspell_command;
        private ConfigEntry<string> config_thunderbolt_command;
        private ConfigEntry<string> config_blast_command;
        private ConfigEntry<string> config_holylight_command;
        private ConfigEntry<string> config_wisp_command;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Mage Arena - Ru loaded!");

            // load config
            config_voiceModel = Config.Bind(
                "General",
                "RuVoiceModel",
                "LanguageModels/vosk-model-small-ru-0.22",
                "Русская модель голоса для распозноания");

            config_fireball_command = Config.Bind(
                "Commands",
                "Fireball",
                "шар;огонь",
                "");

            config_frostbolt_command = Config.Bind(
                "Commands",
                "Frostbolt",
                "заморозка",
                "");

            config_worm_command = Config.Bind(
                "Commands",
                "Worm",
                "вход",
                "");

            config_hole_command = Config.Bind(
                "Commands",
                "Hole",
                "выход",
                "");

            config_magicmissle_command = Config.Bind(
                "Commands",
                "Magic Missle",
                "магия",
                "");

            config_mirror_command = Config.Bind(
                "Commands",
                "Mirror",
                "зеркало",
                "(не понял для чего)");

            config_poofspell_command = Config.Bind(
                "Commands",
                "Blink",
                "телепорт",
                "");

            config_thunderbolt_command = Config.Bind(
                "Commands",
                "Thunderbolt",
                "молния;зевс",
                "");

            config_blast_command = Config.Bind(
                "Commands",
                "Dark Blast",
                "луч",
                "");

            config_holylight_command = Config.Bind(
                "Commands",
                "Holy Light",
                "свет;лечение",
                "");

            config_wisp_command = Config.Bind(
                "Commands",
                "Wisp",
                "висп",
                "");

            ruVoiceModel = config_voiceModel.Value;

            fireball_command = config_fireball_command.Value.Split(';');
            frostbolt_command = config_frostbolt_command.Value.Split(';');
            worm_command = config_worm_command.Value.Split(';');
            hole_command = config_hole_command.Value.Split(';');
            magicmissle_command = config_magicmissle_command.Value.Split(';');
            mirror_command = config_mirror_command.Value.Split(';');
            poofspell_command = config_poofspell_command.Value.Split(';');
            thunderbolt_command = config_thunderbolt_command.Value.Split(';');
            blast_command = config_blast_command.Value.Split(';');
            holylight_command = config_holylight_command.Value.Split(';');
            wisp_command = config_wisp_command.Value.Split(';');
        }

        public void Update()
        {
            if (Input.GetKeyDown(menuOpen))
                openMenu = !openMenu;
            if (Input.GetKeyDown(resetRussian))
                ResetAll();
        }

        public void OnGUI()
        {
            if (!openMenu) return;

            GUI.ModalWindow(0, new Rect(Screen.width / 2 - 200, Screen.height / 2 - 150, 400, 300), delegate (int id)
            {
                if(SettingsHolder.Instance.VoiceModelPath != ruVoiceModel)
                {
                    guioffset = 80;
                    if (GUI.Button(new Rect(10, 20, 380, 60), "Нажмите для первой настройки.\n(и/или если голос не распознаётся)"))
                    {
                        Logger.LogInfo("Applyed RU model");
                        SettingsHolder.Instance.SetVoiceModelPath(ruVoiceModel);
                        Application.Quit();
                    }
                } else
                {
                    guioffset = 30;
                }
                GUI.Label(new Rect(10, guioffset + 5, 380, 45), "F1 - Установить русские команды.\n(Необходимо нажимать при каждой загрузке на локацию)");
                if (GUI.Button(new Rect(10, guioffset + 50, 380, 60), "Установить русские команды\n(Если не работает бинд)"))
                {
                    Logger.LogInfo("Reset to russian models.");
                    ResetAll();
                }
                GUI.Label(new Rect(10, guioffset + 115, 380, 25), $"Распознано: {lastRecognized}");
                GUI.Label(new Rect(10, guioffset + 140, 380, 45), "Если необходимо вернуть английский язык, зайдите\nв меню диалектов и выберите английский.");
            }, "Mage Arena - Ru (Settings)");
        }

        void ResetAll()
        {
            SpeechRecognizer sr = FindFirstObjectByType<SpeechRecognizer>();
            if (sr != null)
            {
                GameObject srobj = sr.gameObject;

                sr.Vocabulary = new List<string>();

                // best shitcode
                foreach (string aliases in fireball_command) sr.Vocabulary.Add(aliases);
                foreach (string aliases in frostbolt_command) sr.Vocabulary.Add(aliases);
                foreach (string aliases in worm_command) sr.Vocabulary.Add(aliases);
                foreach (string aliases in hole_command) sr.Vocabulary.Add(aliases);
                foreach (string aliases in magicmissle_command) sr.Vocabulary.Add(aliases);
                foreach (string aliases in mirror_command) sr.Vocabulary.Add(aliases);
                foreach (string aliases in poofspell_command) sr.Vocabulary.Add(aliases);
                foreach (string aliases in thunderbolt_command) sr.Vocabulary.Add(aliases);
                foreach (string aliases in blast_command) sr.Vocabulary.Add(aliases);
                foreach (string aliases in holylight_command) sr.Vocabulary.Add(aliases);
                foreach (string aliases in wisp_command) sr.Vocabulary.Add(aliases);

                sr.Vocabulary.Add("[не распознано]");

                if (sr != null)
                {
                    sr.ResultReady.AddListener(delegate (Result res)
                    {
                        ProcessInput(res.text);
                    });
                }

                sr.StartProcessing();
            } else
            {
                Logger.LogError("SpeechRecognizer not found!");
            }
        }

        bool checkAliases(string text, string[] aliases)
        {
            foreach (string alias in aliases)
                if (text.Contains(alias)) return true;
            return false;
        }

        public void ProcessInput(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            lastRecognized = text;

            VoiceControlListener vcl = FindFirstObjectByType<VoiceControlListener>(FindObjectsInactive.Exclude);

            if (checkAliases(text, fireball_command)) 
                FindFirstObjectByType<VoiceControlListener>(FindObjectsInactive.Exclude).CastFireball();
            if (checkAliases(text, frostbolt_command)) 
                FindFirstObjectByType<VoiceControlListener>(FindObjectsInactive.Exclude).CastFrostBolt();
            if (checkAliases(text, worm_command)) 
                FindFirstObjectByType<VoiceControlListener>(FindObjectsInactive.Exclude).CastWorm();
            if (checkAliases(text, hole_command)) 
                FindFirstObjectByType<VoiceControlListener>(FindObjectsInactive.Exclude).CastHole();
            if (checkAliases(text, magicmissle_command)) 
                FindFirstObjectByType<VoiceControlListener>(FindObjectsInactive.Exclude).CastMagicMissle();
            if (checkAliases(text, mirror_command)) 
                FindFirstObjectByType<VoiceControlListener>(FindObjectsInactive.Exclude).ActivateMirror();

            using (List<ISpellCommand>.Enumerator enumerator = vcl.SpellPages.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    // best shitcode 2
                    ISpellCommand command = enumerator.Current;
                    if (command.GetSpellName() == "blink" && checkAliases(text, poofspell_command))
                        command.TryCastSpell();
                    else if (command.GetSpellName() == "blast" && checkAliases(text, blast_command))
                        command.TryCastSpell();
                    else if (command.GetSpellName() == "thunderbolt" && checkAliases(text, thunderbolt_command))
                        command.TryCastSpell();
                    else if (command.GetSpellName() == "divine" && checkAliases(text, holylight_command))
                        command.TryCastSpell();
                    else if (command.GetSpellName() == "wisp" && checkAliases(text, wisp_command))
                        command.TryCastSpell();
                }
            }
        }
    }
}
