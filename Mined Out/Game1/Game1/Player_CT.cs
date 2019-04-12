namespace Game1.Entities
{
    class Player_CT : Player
    {
        public bool HasDefuseKit { get; set; }

        public Player_CT(int x, int y) : base(x, y)
        {
            HasDefuseKit = false;
        }

        public void TakeDefuseKit(ref DefuseKit kits)
        {
            HasDefuseKit = true;
            kits = null;
        }
    }
}