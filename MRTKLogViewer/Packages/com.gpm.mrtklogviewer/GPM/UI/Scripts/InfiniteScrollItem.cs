namespace Gpm.Ui
{
    using System;
    using UnityEngine;

    public class InfiniteScrollData
    {
    }


    public class InfiniteScrollItem : MonoBehaviour
    {
        protected InfiniteScrollData                            scrollData          = null;
        protected Action<InfiniteScrollData>                    selectCallback      = null;
        protected Action<InfiniteScrollData, RectTransform>     updateSizeCallback  = null;

        public void AddSelectCallback(Action<InfiniteScrollData> callback)
        {
            selectCallback += callback;
        }

        public void RemoveSelectCallback(Action<InfiniteScrollData> callback)
        {
            selectCallback -= callback;
        }

        public virtual void UpdateData(InfiniteScrollData scrollData)
        {
            this.scrollData = scrollData;
        }

        protected void OnSelect()
        {
            if (selectCallback != null)
            {
                selectCallback(scrollData);
            }
        }

        public void AddUpdateSizeCallback(Action<InfiniteScrollData, RectTransform> callback)
        {
            updateSizeCallback += callback;
        }

        public void RemoveUpdateSizeCallback(Action<InfiniteScrollData, RectTransform> callback)
        {
            updateSizeCallback -= callback;
        }

        protected void OnUpdateItemSize()
        {
            if (updateSizeCallback != null)
            {
                updateSizeCallback(scrollData, transform as RectTransform);
            }
        }
    }
}
