using System;
using System.Collections.Generic;
using Framework.Minigames;

namespace Framework.Minigames.MinigameDefClasses
{
    public abstract class ConfigurableMinigameDefinition : MinigameDefinition
    {
        private static readonly Dictionary<string, string> SceneNameOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ArmDrugge", "armdr\u00fccken" },
            { "BiologyMinigame", "BioScene" },
            { "BossFightMinigame", "BossScene" },
            { "ChemieMinigame1", "chemie_keller" },
            { "ChemieMinigame2", "chemie_keller" },
            { "FindErrorsMinigame", "Find Errors minigame" },
            { "Geography", "quizgamegeography" },
            { "LockpickingMinigame", "lock_picking" },
            { "PianoMinigame", "piano" },
            { "PhMinigame", "PhScene" }
        };
        // Dialogue suffix handling is disabled; dialogue minigames are managed elsewhere.
        private static readonly string[] NameSuffixes = { "Minigame" };

        private readonly string _sceneName;

        protected ConfigurableMinigameDefinition(string sceneNameOverride = null)
        {
            _sceneName = string.IsNullOrWhiteSpace(sceneNameOverride)
                ? ResolveSceneName(GetType().Name)
                : sceneNameOverride;
        }

        protected override string SceneName => _sceneName;

        private static string ResolveSceneName(string typeName)
        {
            if (SceneNameOverrides.TryGetValue(typeName, out var mapped) && !string.IsNullOrWhiteSpace(mapped))
            {
                return mapped;
            }

            var trimmed = typeName;
            foreach (var suffix in NameSuffixes)
            {
                trimmed = TrimSuffix(trimmed, suffix);
            }
            return $"{trimmed}Scene";
        }

        private static string TrimSuffix(string value, string suffix)
        {
            if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return value.Substring(0, value.Length - suffix.Length);
            }

            return value;
        }
    }

    // ========== Dialogue-specific Minigame Definitions ==========
    // These classes all load the same "Dialogue" scene but with different minigame IDs.
    // The DialogueManager will determine which dialogue to show based on the minigame ID and phase.

    public abstract class DialogueMinigameDefinition : MinigameDefinition
    {
        protected abstract string MinigameID { get; }
        protected virtual string MinigameSceneName => null;  // Override this if dialogue should chain to a minigame
        protected override string SceneName => "Dialogue";

        public override bool Launch(MinigameLaunchContext context)
        {
            UnityEngine.Debug.Log($"[DialogueMinigame] Launching dialogue for '{MinigameID}'");
            
            // Determine the dialogue phase based on game progress
            string phase = DetermineDialoguePhase(MinigameID);

            UnityEngine.Debug.Log($"[DialogueMinigame] Phase determined: {phase}");
            UnityEngine.Debug.Log($"[DialogueMinigame] Current scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            UnityEngine.Debug.Log($"[DialogueMinigame] Return slide key: {context.ReturnSlideKey}");
            
            // Determine next scene: If Start phase and MinigameSceneName is set, go to minigame after dialogue
            string nextScene = (phase == "Start" && !string.IsNullOrWhiteSpace(MinigameSceneName)) ? MinigameSceneName : null;
            
            if (nextScene != null)
            {
                UnityEngine.Debug.Log($"[DialogueMinigame] Will chain to minigame scene: {nextScene}");
            }

            // Store transition data for the Dialogue scene to pick up
            DialogueTransitionData.SetTransitionData(
                minigameID: MinigameID,
                phase: phase,
                returnSceneName: UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                returnSlideKey: context.ReturnSlideKey,
                nextMinigameScene: nextScene
            );

            // Load the Dialogue scene
            UnityEngine.Debug.Log($"[DialogueMinigame] Attempting to load scene: {SceneName}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName, LoadMode);
            return true;
        }

        /// <summary>
        /// Determines the dialogue phase (Start, Retry, Win, Loose) based on game progress data.
        /// </summary>
        private string DetermineDialoguePhase(string minigameID)
        {
            // Try to get the progress data for this minigame
            if (GameDataLoader.TryGetMinigameProgress(minigameID, out bool completed, out int tries))
            {
                if (completed)
                {
                    return "Win";
                }
                else if (tries > 0)
                {
                    return "Retry";
                }
                else
                {
                    return "Start";
                }
            }

            // Default to Start if no progress data found
            return "Start";
        }
    }

    public class PhDialogue : DialogueMinigameDefinition
    {
        protected override string MinigameID => "Ph";
        // Forum dialogues don't chain to minigames - they're just conversations
    }

    public class BgDialogue : DialogueMinigameDefinition
    {
        protected override string MinigameID => "BgDialogue";
        // Forum dialogues don't chain to minigames - they're just conversations
    }

    public class BioDialogue : DialogueMinigameDefinition
    {
        protected override string MinigameID => "Bio";
        // Forum dialogues don't chain to minigames - they're just conversations
    }

    public class GeoDialogue : DialogueMinigameDefinition
    {
        protected override string MinigameID => "Geo";
        // Forum dialogues don't chain to minigames - they're just conversations
    }

    public class JacketDialogue : DialogueMinigameDefinition
    {
        protected override string MinigameID => "Jacket";
        // Conversation dialogue
    }

    public class DimitriDialogue : DialogueMinigameDefinition
    {
        protected override string MinigameID => "Dimitri";
        // Conversation dialogue
    }

    public class PianoDialogue : DialogueMinigameDefinition
    {
        protected override string MinigameID => "Piano";
        // No MinigameSceneName - Piano game triggers its own dialogue, should return to navigation
    }

    public class ArmdruggeDialogueStart : DialogueMinigameDefinition
    {
        protected override string MinigameID => "Armdrugge";
        protected override string MinigameSceneName => "armdr\u00fccken";  // Scene to load after Start dialogue
    }

    public class ArmdruggeDialogueWin : DialogueMinigameDefinition
    {
        protected override string MinigameID => "Armdrugge";
        // No MinigameSceneName - Win dialogue returns to navigation

        public override bool Launch(MinigameLaunchContext context)
        {
            // Force the phase to be "Win" for this specific dialogue trigger
            DialogueTransitionData.SetTransitionData(
                minigameID: "Armdrugge",
                phase: "Win",
                returnSceneName: UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                returnSlideKey: context.ReturnSlideKey,
                nextMinigameScene: null
            );

            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName, LoadMode);
            return true;
        }
    }

    public class ArmdruggeDialogueLoose : DialogueMinigameDefinition
    {
        protected override string MinigameID => "Armdrugge";
        // No MinigameSceneName - Loose dialogue returns to navigation

        public override bool Launch(MinigameLaunchContext context)
        {
            // Force the phase to be "Loose" for this specific dialogue trigger
            DialogueTransitionData.SetTransitionData(
                minigameID: "Armdrugge",
                phase: "Loose",
                returnSceneName: UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                returnSlideKey: context.ReturnSlideKey,
                nextMinigameScene: null
            );

            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName, LoadMode);
            return true;
        }
    }
}
