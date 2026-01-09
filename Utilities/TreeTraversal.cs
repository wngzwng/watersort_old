
namespace Utilities;

/// <summary>
/// 通用树形结构遍历工具
/// </summary>
public static class TreeTraversal
{
    /// <summary>
    /// 深度优先遍历（先序遍历）
    /// <typeparam name="T">节点类型</typeparam>
    /// <param name="root">根节点</param>
    /// <param name="getChildren">获取子节点的方法</param>
    /// <param name="callback">节点回调函数，返回false可中断遍历</param>
    /// <returns>遍历是否完成（未被中断）</returns>
    /// </summary>
    public static bool DepthFirstTraversal<T>(T root, Func<T, IEnumerable<T>> getChildren, Func<T, bool> callback)
    {
        if (root == null) return true;
        
        // 先处理当前节点
        if (!callback(root))
            return false;
        
        // 递归处理子节点
        var children = getChildren(root);
        if (children != null)
        {
            foreach (var child in children)
            {
                if (!DepthFirstTraversal(child, getChildren, callback))
                    return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// 广度优先遍历
    /// </summary>
    public static bool BreadthFirstTraversal<T>(T root, Func<T, IEnumerable<T>> getChildren, Func<T, bool> callback)
    {
        if (root == null) return true;
        
        var queue = new Queue<T>();
        queue.Enqueue(root);
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            
            if (!callback(current))
                return false;
                
            var children = getChildren(current);
            if (children != null)
            {
                foreach (var child in children)
                {
                    queue.Enqueue(child);
                }
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 在树中查找节点（深度优先）
    /// </summary>
    public static T FindNode<T>(T root, Func<T, IEnumerable<T>> getChildren, Func<T, bool> predicate, Func<T, bool> shouldPrune = null, T defaultValue = default(T))
    {
        T result = defaultValue;
    
        DepthFirstTraversal(root, 
            node => shouldPrune != null && shouldPrune(node) 
                ? Enumerable.Empty<T>() 
                : getChildren(node),
            node =>
            {
                if (predicate(node))
                {
                    result = node;
                    return false;
                }
                return true;
            });
    
        return result;
    }
    
    /// <summary>
    /// 查找所有满足条件的节点
    /// </summary>
    public static List<T> FindAllNodes<T>(T root, Func<T, IEnumerable<T>> getChildren, Func<T, bool> predicate)
    {
        var results = new List<T>();
        
        DepthFirstTraversal(root, getChildren, node =>
        {
            if (predicate(node))
            {
                results.Add(node);
            }
            return true; // 继续遍历所有节点
        });
        
        return results;
    }
    
    /// <summary>
    /// 将树转换为平面列表（深度优先顺序）
    /// </summary>
    public static List<T> Flatten<T>(T root, Func<T, IEnumerable<T>> getChildren)
    {
        var list = new List<T>();
        DepthFirstTraversal(root, getChildren, node =>
        {
            list.Add(node);
            return true;
        });
        return list;
    }
}