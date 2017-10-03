namespace DungeonGeek
{
    interface INamable
    {
        string ReservedTextForTitle { get; }

        void Rename(string newName);

    }
}
