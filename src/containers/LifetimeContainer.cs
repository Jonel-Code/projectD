using System.Collections.Generic;
using System.Runtime.InteropServices;

struct LifetimeContainerData<T>
{
    public T Value;
    public double Lifetime;
}

public delegate void UpdateLifetimeContent<T>(ref T value, double delta);
public delegate void OnLifetimeContentRemove<T>(ref T value, double? delta, double? lifetime);

partial class LifetimeContainer<T>
{
    protected List<LifetimeContainerData<T>> Container = new();

    protected Queue<int> ToRemove = new();

    public UpdateLifetimeContent<T> UpdateContentDelagate { get; set; }

    public OnLifetimeContentRemove<T> OnRemoveDelegate { get; set; }

    private bool IsDestroyed { get; set; }

    public void Reserve(int count)
    {
        Container.Capacity = count;
    }

    public void Destroy()
    {
        IsDestroyed = true;
    }

    public void Add(T value, double lifetime)
    {
        Container.Add(new LifetimeContainerData<T> { Value = value, Lifetime = lifetime });
    }

    public void UpdateLifetime(double delta)
    {
        if (!IsDestroyed)
        {
            var span = CollectionsMarshal.AsSpan(Container);
            if (span.Length > 0)
            {
                for (int i = span.Length - 1; i >= 0; i--)
                {
                    span[i].Lifetime -= delta;
                    UpdateContentDelagate.Invoke(ref span[i].Value, delta);
                    if (span[i].Lifetime <= 0)
                    {
                        OnRemoveDelegate.Invoke(ref span[i].Value, delta, span[i].Lifetime);
                        ToRemove.Enqueue(i);
                    }
                }
            }
        }

        while (ToRemove.Count > 0)
        {
            var i = ToRemove.Dequeue();
            Container.RemoveAt(i);
        }
    }
}