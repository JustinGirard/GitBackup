namespace MDPackage.Utilities
{
    public sealed class MD_BackendGenericButton
    {
        public bool IsUp { get; private set; }
        public bool HasUpChanged { get; private set; }

        public bool IsDown { get; private set; }
        public bool HasDownChanged { get; private set; }

        public MD_BackendGenericButton()
        {
            HasUpChanged = true;
            HasDownChanged = true;
        }

        public void SyncButton(bool state)
        {
            if (HasDownChanged != state)
                IsDown = HasDownChanged = state;
            if (HasUpChanged != !state)
                IsUp = HasUpChanged = !state;
        }

        public void ResetButton(bool toValue)
        {
            IsDown = toValue;
            IsUp = toValue;
        }
    }
}