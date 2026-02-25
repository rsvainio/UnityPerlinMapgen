using System;

// could potentially make this support both max and min heaps if needed
public class Heap<T> where T : IHeapItem<T>
{
    public bool maxHeap { get; private set; }
    public int Count => _currentItemCount;

    private T[] _heap;
    private int _currentItemCount = 0;

    public T Parent(T item) => (item.heapIndex - 1) / 2 == 1 ? default : _heap[(item.heapIndex - 1) / 2];
    public T Left(T item) => item.heapIndex * 2 + 1 <= _currentItemCount ? _heap[item.heapIndex * 2 + 1] : default;
    public T Right(T item) => item.heapIndex * 2 + 2 <= _currentItemCount ? _heap[item.heapIndex * 2 + 2] : default;

    public Heap(int maxHeapSize)
    {
        _heap = new T[maxHeapSize];
    }

    public void Insert(T item)
    {
        item.heapIndex = _currentItemCount;
        _heap[item.heapIndex] = item;
        SortUp(item);
        _currentItemCount++;
    }

    public T ExtractFirst()
    {
        T firstItem = _heap[0];
        _currentItemCount--;

        _heap[0] = _heap[_currentItemCount];
        _heap[0].heapIndex = 0;
        SortDown(firstItem);

        return firstItem;
    }

    public void UpdateItem(T item) => SortUp(item);

    public bool Contains(T item)
    {
        return Equals(_heap[item.heapIndex], item);
    }

    private void SortDown(T item)
    {
        while (true)
        {
            T leftChild = Left(item);
            T rightChild = Right(item);
            T itemToSwap;

            if (leftChild.heapIndex < _currentItemCount)
            {
                itemToSwap = leftChild;

                if (rightChild.heapIndex < _currentItemCount)
                {
                    if (leftChild.CompareTo(rightChild) < 0)
                    {
                        itemToSwap = rightChild;
                    }
                }

                // swap the items if item is lower than itemToSwap
                if (item.CompareTo(itemToSwap) < 0)
                {
                    Swap(item, itemToSwap);
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
    }

    private void SortUp(T item)
    {
        while (true)
        {
            T parent = Parent(item);

            if (item.CompareTo(parent) > 0) // need to check for null
            {
                Swap(item, parent);
            }
            else
            {
                return;
            }
        }
    }

    private void Swap(T itemA, T itemB)
    {
        _heap[itemA.heapIndex] = itemB;
        _heap[itemB.heapIndex] = itemA;

        (itemA.heapIndex, itemB.heapIndex) = (itemB.heapIndex, itemA.heapIndex);
    }
}

public interface IHeapItem<T> : IComparable<T>
{
    int heapIndex { get; set; }
}
