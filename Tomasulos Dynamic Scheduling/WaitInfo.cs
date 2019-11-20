namespace Tomasulo
{
    public class WaitInfo
    {
        //enum 枚举
        public enum WaitState { LoadStation, StoreStation, MultStation, AddStation, Compute, Avail, LoadMem, StoreMem, ReorderBuffer };

        public float value;
        public WaitState waitState = new WaitState();

        public WaitInfo(float v, WaitState w)
        {
            value = v;
            waitState = w;
        }
    }
}