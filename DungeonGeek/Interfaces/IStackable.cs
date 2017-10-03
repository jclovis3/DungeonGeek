namespace DungeonGeek
{
    interface IStackable
    {
        int Qty { get; } // Quantity of items in stack - implies need for a private qty variable.

        IStackable Remove(); // Pull one from the stack and return it

        void Add(int qty=1); // Add one or more to the stack

        
    }
}
