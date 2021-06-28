namespace Gpm.Ui
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    public class InfiniteScroll : MonoBehaviour
    {
        public enum MoveToType
        {
            MOVE_TO_TOP = 0,
            MOVE_TO_CENTER,
            MOVE_TO_BOTTOM
        }

        public InfiniteScrollItem               itemPrefab              = null;
        public int                              padding                 = 0;
        public int                              space                   = 0;
        public bool                             dynamicItemSize         = false;

        protected bool                          isInitialize            = false;
        protected ScrollRect                    scrollRect              = null;
        protected RectTransform                 content                 = null;
        protected RectTransform                 viewport                = null;
        protected bool                          isVertical              = false;
        protected Vector2                       anchorMin               = Vector2.zero;
        protected Vector2                       anchorMax               = Vector2.zero;
        protected List<InfiniteScrollData>      dataList                = new List<InfiniteScrollData>();
        protected float                         defaultItemSize         = 0.0f;
        protected int                           needItemNumber          = -1;
        protected int                           madeItemNumber          = 0;
        protected List<InfiniteScrollItem>      items                   = null;
        protected int[]                         itemShowDataIndex       = null;
        protected float                         firstItemPosition       = 0.0f;
        protected int                           selectDataIndex         = -1;
        protected Action<InfiniteScrollData>    selectCallback          = null;
        protected float                         sizeInterpolationValue  = 0.0001f; // 0.01%
        protected List<float>                   itemSizeList            = new List<float>();
        protected float                         minItemSize             = 0.0f;

        public bool IsMoveToLastData()
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            float viewportSize  = 0.0f;
            float contentSize   = 0.0f;
            float position      = 0.0f;

            if (isVertical == true)
            {
                viewportSize    = viewport.rect.height;
                contentSize     = content.rect.height;
                position        = content.anchoredPosition.y;
            }
            else
            {
                viewportSize    = viewport.rect.width;
                contentSize     = content.rect.width;
                position        = -content.anchoredPosition.x;
            }

            return IsMoveToLastData(position, viewportSize, contentSize);
        }

        private bool IsMoveToLastData(float position, float viewportSize, float contentSize)
        {
            bool isShow = false;

            if (viewportSize > contentSize)
            {
                isShow = true;
            }
            else
            {
                float interpolation = contentSize * sizeInterpolationValue;
                if (Mathf.Abs(position + viewportSize - contentSize) <= interpolation)
                {
                    isShow = true;
                }
            }

            return isShow;
        }

        public void ResizeScrollView()
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            CheckNeedMoreItem();
        }

        public void MoveTo(InfiniteScrollData data, MoveToType moveToType)
        {
            MoveTo(GetDataIndex(data), moveToType);
        }

        public void MoveTo(int dataIndex, MoveToType moveToType)
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            if (IsValidDataIndex(dataIndex) == true)
            {
                Vector2 prevPosition    = content.anchoredPosition;
                float   move            = 0.0f;

                if (isVertical == true)
                {
                    move = GetMovePosition(dataIndex, viewport.rect.height, content.rect.height, moveToType);
                    content.anchoredPosition = new Vector2(prevPosition.x, move);
                }
                else
                {
                    move = GetMovePosition(dataIndex, viewport.rect.width, content.rect.width, moveToType);
                    content.anchoredPosition = new Vector2(-move, prevPosition.y);
                }
            }
        }

        private float GetMovePosition(int dataIndex, float viewportSize, float contentSize, MoveToType moveToType)
        {
            float move              = 0.0f;
            float moveItemSize      = GetItemSize(dataIndex);
            float passingItemSize   = GetItemSizeSum(dataIndex);

            move = passingItemSize + padding;

            switch (moveToType)
            {
                case MoveToType.MOVE_TO_CENTER:
                    {
                        move -= viewportSize * 0.5f - moveItemSize * 0.5f;
                        break;
                    }
                case MoveToType.MOVE_TO_BOTTOM:
                    {
                        move -= viewportSize - moveItemSize;
                        break;
                    }
            }

            move = Mathf.Clamp(move, 0.0f, contentSize - viewportSize);
            move = Math.Max(0.0f, move);

            return move;
        }

        public void MoveToFirstData()
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            if (isVertical == true)
            {
                scrollRect.normalizedPosition = Vector2.one;
            }
            else
            {
                scrollRect.normalizedPosition = Vector2.zero;
            }
        }

        public void MoveToLastData()
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            if (isVertical == true)
            {
                scrollRect.normalizedPosition = Vector2.zero;
            }
            else
            {
                scrollRect.normalizedPosition = Vector2.one;
            }
        }

        public void AddSelectCallback(Action<InfiniteScrollData> callback)
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            selectCallback += callback;
        }

        public void RemoveSelectCallback(Action<InfiniteScrollData> callback)
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            selectCallback -= callback;
        }

        public void AddScrollValueChangedLisnter(UnityAction<Vector2> listener)
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            scrollRect.onValueChanged.AddListener(listener);
        }

        public void InsertData(InfiniteScrollData data)
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            dataList.Add(data);

            CreateItem(data);

            itemSizeList.Add(defaultItemSize);
            
            ResizeContent();

            UpdateShowItem();
        }

        public void RemoveData(InfiniteScrollData data)
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            int dataIndex = GetDataIndex(data);

            RemoveData(dataIndex);
        }

        public void RemoveData(int dataIndex)
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            if (IsValidDataIndex(dataIndex) == true)
            {
                selectDataIndex = -1;
                dataList.RemoveAt(dataIndex);
                itemSizeList.RemoveAt(dataIndex);
                ResizeContent();
                UpdateShowItem(true);
            }
        }

        public void Clear()
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            dataList.Clear();
            itemSizeList.Clear();
            ResizeContent();
            ResetItemShowDataIndex();
            SetItemActive(false);

            selectDataIndex = -1;
        }

        public int GetDataCount()
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            return dataList.Count;
        }

        public InfiniteScrollData GetData(int index)
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            if (IsValidDataIndex(index) == true)
            {
                return dataList[index];
            }
            else
            {
                return null;
            }
        }

        public void UpdateData(InfiniteScrollData data)
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            int dataIndex = GetDataIndex(data);
            if (IsValidDataIndex(dataIndex) == true)
            {
                int itemIndex = GetItemIndexByDataIndex(GetShowFirstDataIndex(), dataIndex);
                if (itemIndex != -1)
                {
                    items[itemIndex].UpdateData(data);
                }
            }
        }

        public void UpdateAllData()
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            UpdateShowItem(true);
        }

        public int GetDataIndex(InfiniteScrollData data)
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            return dataList.FindIndex(p => p.Equals(data));
        }

        private void Awake()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            if (isInitialize == false)
            {
                scrollRect = GetComponent<ScrollRect>();
                content = scrollRect.content;
                viewport = scrollRect.viewport;
                isVertical = scrollRect.vertical;

                if (isVertical == true)
                {
                    anchorMin = new Vector2(content.anchorMin.x, 1.0f);
                    anchorMax = new Vector2(content.anchorMax.x, 1.0f);

                    content.anchorMin = anchorMin;
                    content.anchorMax = anchorMax;
                    content.pivot = new Vector2(0.5f, 1.0f);
                }
                else
                {
                    anchorMin = new Vector2(0, content.anchorMin.y);
                    anchorMax = new Vector2(0, content.anchorMax.y);

                    content.anchorMin = anchorMin;
                    content.anchorMax = anchorMax;
                    content.pivot = new Vector2(0.0f, 0.5f);
                }

                dataList.Clear();
                itemSizeList.Clear();

                scrollRect.onValueChanged.AddListener(OnValueChanged);

                isInitialize = true;
            }
        }

        private void OnSelectItem(InfiniteScrollData data)
        {
            int dataIndex = GetDataIndex(data);
            if (IsValidDataIndex(dataIndex) == true)
            {
                selectDataIndex = dataIndex;

                if (selectCallback != null)
                {
                    selectCallback(data);
                }
            }
        }

        private bool IsValidDataIndex(int index)
        {
            return (index >= 0 && index < dataList.Count) ? true : false;
        }

        private void CreateItem(InfiniteScrollData data, bool isActive = true)
        {            
            if (madeItemNumber == needItemNumber)
            {
                return;
            }

            if (madeItemNumber > dataList.Count)
            {
                return;
            }

            InfiniteScrollItem  item            = Instantiate(itemPrefab);
            RectTransform       itemTransform   = (RectTransform)item.transform;
            
            itemTransform.anchorMin             = anchorMin;
            itemTransform.anchorMax             = anchorMax;
            itemTransform.pivot                 = content.pivot;

            if (isVertical == true)
            {
                itemTransform.sizeDelta = new Vector2(0, itemTransform.sizeDelta.y);
            }
            else
            {
                itemTransform.sizeDelta = new Vector2(itemTransform.sizeDelta.x, 0);
            }

            itemTransform.SetParent(content, false);
            itemTransform.gameObject.SetActive(isActive);
            
            if (madeItemNumber == 0)
            {
                InitializeItemInformation(itemTransform);
            }

            ++madeItemNumber;

            items.Add(item);
            item.AddSelectCallback(OnSelectItem);

            if (dynamicItemSize == true)
            {
                item.AddUpdateSizeCallback(OnUpdateItemSize);
            }
        }

        private void InitializeItemInformation(RectTransform itemTransform)
        {
            if (isVertical == true)
            {
                defaultItemSize     = itemTransform.rect.height;
            }
            else
            {
                defaultItemSize     = itemTransform.rect.width;
            }

            SetFirstItemPosition(defaultItemSize, itemTransform.pivot);

            minItemSize     = defaultItemSize;
            needItemNumber  = GetNeedItemNumber();

            items = new List<InfiniteScrollItem>();
            ResetItemShowDataIndex();
        }

        private void CheckNeedMoreItem()
        {
            int itemNumber  = GetNeedItemNumber();

            if (needItemNumber < itemNumber)
            {
                int gap = itemNumber - needItemNumber;
                needItemNumber = itemNumber;

                if (dataList.Count > 0)
                {
                    int firstDataIndex  = GetShowFirstDataIndex() + madeItemNumber;
                    int dataIndex       = 0;

                    for (int count = 0; count < gap; ++count)
                    {
                        dataIndex = firstDataIndex + count;
                        if (IsValidDataIndex(dataIndex) == true)
                        {
                            CreateItem(dataList[dataIndex]);
                        }
                        else
                        {
                            CreateItem(dataList[0], false);
                        }
                    }
                }

                ResetItemShowDataIndex();

                UpdateShowItem(true);
            }
        }

        private void ResetItemShowDataIndex()
        {
            if(needItemNumber > -1 )
            {
                itemShowDataIndex = Enumerable.Repeat<int>(-1, needItemNumber).ToArray();
            }
        }

        private void SetItemActive(bool active)
        {
            if (items == null)
            {
                return;
            }

            for (int index = 0; index < items.Count; ++index)
            {
                items[index].gameObject.SetActive(active);
            }
        }
        
        private void ResizeContent()
        {
            Vector2 currentSize = content.sizeDelta;
            float   size        = GetContentSize();

            if (isVertical == true)
            {
                content.sizeDelta = new Vector2(currentSize.x, size);
            }
            else
            {
                content.sizeDelta = new Vector2(size, currentSize.y);
            }
        }

        private float GetContentSize()
        {
            float itemTotalSize = GetItemSizeSum(itemSizeList.Count);

            return itemTotalSize + padding * 2.0f;
        }

        private void UpdateShowItem(bool forceUpdateData = false)
        {
            int             firstDataIndex  = GetShowFirstDataIndex();
            RectTransform   itemTransform   = null;
            int             itemIndex       = 0;
            
            InfiniteScrollItem item = null;
            for (int dataIndex = firstDataIndex; dataIndex < firstDataIndex + madeItemNumber; ++dataIndex)
            {
                itemIndex   = GetItemIndexByDataIndex(firstDataIndex, dataIndex);
                item        = items[itemIndex];
                
                if (dataIndex >= dataList.Count)
                {
                    item.gameObject.SetActive(false);
                }
                else
                {
                    itemTransform = (RectTransform)item.transform;
                    
                    SetItemSizeAndPosition(itemTransform, dataIndex);

                    bool needUpdateData = false;

                    if (item.gameObject.activeSelf == false)
                    {
                        item.gameObject.SetActive(true);
                        needUpdateData = true;
                    }

                    if (itemShowDataIndex[itemIndex] != dataIndex)
                    {
                        itemShowDataIndex[itemIndex] = dataIndex;
                        needUpdateData = true;
                    }

                    if (needUpdateData == true || forceUpdateData == true)
                    {
                        item.UpdateData(dataList[dataIndex]);
                    }
                }
            }
        }

        private void SetItemSizeAndPosition(RectTransform rectTransform, int dataIndex)
        {
            float   passingItemSize     = GetItemSizeSum(dataIndex);
            float   size                = GetItemSize(dataIndex);

            Vector2 currentSize         = rectTransform.sizeDelta;
            
            if (isVertical == true)
            {
                rectTransform.sizeDelta         = new Vector2(currentSize.x, size);
                rectTransform.anchoredPosition  = new Vector2(0, firstItemPosition - passingItemSize);
            }
            else
            {
                rectTransform.sizeDelta         = new Vector2(size, currentSize.y);
                rectTransform.anchoredPosition  = new Vector2(firstItemPosition + passingItemSize, 0);
            }
        }

        private int GetItemIndexByDataIndex(int firstDataIndex, int dataIndex)
        {
            int itemIndex       = 0;
            int defaultIndex    = -1;
            int findIndex       = -1;

            for (int index = 0; index < itemShowDataIndex.Length; ++index)
            {
                if (itemShowDataIndex[index] == dataIndex)
                {
                    findIndex = index;
                    break;
                }

                if(IsShowDataIndex(firstDataIndex, itemShowDataIndex[index]) == false)
                {
                    itemShowDataIndex[index] = -1;
                }

                if (itemShowDataIndex[index] == -1 && defaultIndex == -1)
                {
                    defaultIndex = index;
                }
            }

            if (findIndex != -1)
            {
                itemIndex = findIndex;
            }
            else
            {
                itemIndex = defaultIndex;
            }

            return itemIndex;
        }

        private bool IsShowDataIndex(int firstDataIndex, int dataIndex)
        {
            if (dataIndex >= firstDataIndex && dataIndex < firstDataIndex + madeItemNumber)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private int GetShowFirstDataIndex()
        {
            int     index           = -1;
            float   contentPosition = 0;
            float   itemSizeSum     = 0.0f;

            if (isVertical == true)
            {
                contentPosition = content.anchoredPosition.y;
            }
            else
            {
                contentPosition = -content.anchoredPosition.x;
            }

            if (dynamicItemSize == true)
            {
                for (int sizeIndex = 0; sizeIndex < itemSizeList.Count; ++sizeIndex)
                {
                    itemSizeSum += itemSizeList[sizeIndex];

                    if (sizeIndex + 1 < itemSizeList.Count)
                    {
                        itemSizeSum += space;
                    }

                    if (itemSizeSum >= contentPosition)
                    {
                        index = sizeIndex;
                        break;
                    }
                }
            }
            else
            {
                index = (int)(contentPosition / (defaultItemSize + space));
            }

            if (index < 0)
            {
                index = 0;
            }

            return index;
        }    
        
        private void OnValueChanged(Vector2 value)
        {
            UpdateShowItem();
        }

        private void OnUpdateItemSize(InfiniteScrollData data, RectTransform itemTransform)
        {
            int dataIndex = GetDataIndex(data);

            if (IsValidDataIndex(dataIndex) == true)
            {
                float size = 0.0f;

                if (isVertical == true)
                {
                    size = itemTransform.rect.height;
                }
                else
                {
                    size = itemTransform.rect.width;
                }

                if (dataIndex == 0)
                {
                    SetFirstItemPosition(size, itemTransform.pivot);
                }

                itemSizeList[dataIndex] = size;

                ResizeContent();

                if (minItemSize > size)
                {
                    minItemSize = size;
                    CheckNeedMoreItem();
                }
                else
                {
                    UpdateShowItem();
                }
            }
        }

        private float GetItemSizeSum(int toIndex)
        {
            float sizeSum = 0.0f;
           
            if (dynamicItemSize == true)
            {
                for (int index = 0; index < toIndex; ++index)
                {
                    sizeSum += itemSizeList[index];
                }
            }
            else
            {
                sizeSum = defaultItemSize * toIndex;
            }

            if (toIndex > 0)
            {
                int spaceCount = toIndex;
                if (toIndex == itemSizeList.Count)
                {
                    spaceCount--;
                }

                sizeSum = sizeSum + space * spaceCount;
            }

            return sizeSum;
        }

        private float GetItemSize(int dataIndex)
        {
            float size = 0.0f;

            if (dynamicItemSize == true)
            {
                size = itemSizeList[dataIndex];
            }
            else
            {
                size = defaultItemSize;
            }

            return size;
        }

        private void SetFirstItemPosition(float itemSize, Vector2 pivot)
        {
            if (isVertical == true)
            {
                firstItemPosition = itemSize * pivot.y - itemSize - padding;
            }
            else
            {
                firstItemPosition = itemSize * pivot.x + padding;
            }
        }

        private int GetNeedItemNumber()
        {
            int needItemNumber = 0;

            float itemSize = 0.0f;
            if (dynamicItemSize == true)
            {
                itemSize = minItemSize;
            }
            else
            {
                itemSize = defaultItemSize;
            }

            if (isVertical == true)
            {
                needItemNumber = (int)(viewport.rect.height / itemSize) + 2;
            }
            else
            {
                needItemNumber = (int)(viewport.rect.width / itemSize) + 2;
            }

            return needItemNumber;
        }
    }
}
