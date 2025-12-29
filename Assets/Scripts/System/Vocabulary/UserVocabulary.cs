using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class UserVocabulary
{
    public List<UserWordData> words = new List<UserWordData>();

    public bool HasWord(string word)
    {
        return words.Exists(w => w.word == word);
    }

    public void AddNewWord(string word)
    {
        words.Add(new UserWordData
        {
            word = word,
            memoryScore = 30,
            seenCount = 1,
            lastSeen = DateTime.UtcNow.Ticks
        });
    }

    public void IncreaseSeenCount(string word)
    {
        var w = words.Find(x => x.word == word);
        if (w == null) return;

        w.seenCount++;
        w.lastSeen = DateTime.UtcNow.Ticks;
    }

    public void IncreaseMemory(string word)
    {
        var w = words.Find(x => x.word == word);
        if (w == null) return;

        w.memoryScore = Math.Min(100, w.memoryScore + 10);
    }

    public void DecreaseMemory(string word)
    {
        var w = words.Find(x => x.word == word);
        if (w == null) return;

        w.memoryScore = Math.Max(0, w.memoryScore - 10);
    }

    public void SetMastered(string word, bool mastered)
    {
        var w = words.Find(x => x.word == word);
        if (w == null) return;

        w.memoryScore = mastered ? 100 : 30; // Reset to 30 if unmastered
    }

    public IEnumerable<string> GetWordsSortedByMemory()
    {
        return words
            .OrderBy(w => w.memoryScore)
            .ThenBy(w => w.lastSeen)
            .Select(w => w.word);
    }
}
