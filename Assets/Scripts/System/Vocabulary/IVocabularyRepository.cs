public interface IVocabularyRepository
{
    UserVocabulary Load();
    void Save(UserVocabulary data);
}