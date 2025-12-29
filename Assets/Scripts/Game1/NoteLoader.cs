using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[System.Serializable]
public class RoleInfo
{
    public string word;
    public string role;
    public string buttonBg;
}

public class NoteLoader : MonoBehaviour
{
    public List<NoteData> notes = new List<NoteData>();

    void Awake()
    {
        notes.Clear();

        var dataManager = GameDataManager.Instance;
        var step = dataManager.CurrentStep ?? StepResourceResolver.CreateFallbackStep();
        if (dataManager.CurrentStep == null)
        {
            dataManager.CurrentStep = step;
        }

        var chapterId = string.IsNullOrEmpty(dataManager.CurrentChapterId)
            ? StepResourceResolver.GetFallbackChapterId()
            : dataManager.CurrentChapterId;
        dataManager.CurrentChapterId = chapterId;

        var wordToBg = new Dictionary<string, string>();
        TextAsset roleAsset = StepResourceResolver.LoadRoleAsset(chapterId, step);
        if (roleAsset != null)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<Wrapper<RoleInfo>>(WrapArray(roleAsset.text));
                if (wrapper?.items != null)
                {
                    foreach (var role in wrapper.items)
                    {
                        var normalizedKey = role.word?.Trim();
                        if (string.IsNullOrEmpty(normalizedKey))
                        {
                            continue;
                        }

                        var bgKey = StepResourceResolver.NormalizeButtonBackgroundKey(role.buttonBg);
                        wordToBg[normalizedKey] = bgKey;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[NoteLoader] Failed to parse role mapping for {chapterId}/{step.id}: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[NoteLoader] Role mapping not found for {chapterId}/{step.id}.");
        }

        TextAsset noteAsset = StepResourceResolver.LoadLyricsAsset(chapterId, step);
        if (noteAsset == null)
        {
            Debug.LogError($"[NoteLoader] Timeline JSON not found for {chapterId}/{step.id}.");
            return;
        }

        List<AlignWord> data;
        try
        {
            data = JsonConvert.DeserializeObject<List<AlignWord>>(noteAsset.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NoteLoader] Failed to parse align data for {chapterId}/{step.id}: {ex.Message}");
            return;
        }

        foreach (var word in data)
        {
            if (word == null || string.IsNullOrEmpty(word.word))
            {
                continue;
            }

            var lookupKey = word.word.Trim();
            if (!wordToBg.TryGetValue(lookupKey, out var buttonBg))
            {
                buttonBg = StepResourceResolver.DefaultButtonBg;
            }

            buttonBg = StepResourceResolver.NormalizeButtonBackgroundKey(buttonBg);

            notes.Add(new NoteData
            {
                word = word.word,
                startTime = word.start,
                buttonBg = buttonBg
            });
        }

        Debug.Log($"[NoteLoader] Loaded {notes.Count} notes for {chapterId}/{step.id}.");
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] items;
    }

    private static string WrapArray(string json)
    {
        return "{\"items\":" + json + "}";
    }
}