> U: Uniform 等长  </br>
> LS: LongShort 长短试管  </br>
> P:  Prop 道具


### 求解
##### 1. UNP 关卡求解   TBS
##### 2. UP 关卡求解   TBS微调
- 帷幕：完成特定颜色 c
  - tbs完成条件： F_c = h && G_c = 0
- 机械臂-固定：只能接受，不能倒出
  - a. 未倒入颜色，则指定颜色，作为初始状态；【有几种颜色，就有几种初始状态】
  - b. 有倒入颜色，则指定为已经倒入的颜色
  - tbs方式：机械臂作为指定颜色的 G_c 的扩充空间
- 石膏：旁侧有完成屏即可解锁（旁侧需要布局来确定，无布局默认两旁）
  - tbs方式：
    - 旁侧 tbs = 0
    - 有颜色完成
    - 存在一个空瓶（用来转移到完成屏到石膏旁侧）
- 钥匙和锁： 钥匙所在液体暴露获得，然后解锁锁
  - 确定钥匙所在的颜色段（用于钥匙所在顶部边界的判定）
  - 钥匙距离瓶顶的高度 diff-h
  - 钥匙上方可能有同色液体数量  diff-old-boundary
  - tsb：
    - G_c - F_c == diff-h 的情况下：
      - 同色液体局部转移可获得钥匙
    - 其余情况：该顶部边界下降即可获得钥匙
- 专属颜色: 只有特定颜色可放入
  - tsb: 对应颜色 G_c + h 
#### 3. 通用求解器
- 目标： 每次移动都推进状态 or 紧凑化操作
- 转变： 每次不在以移动一次作为边，而是推动状态变化的移动组为边
- 灵感： 
  - 顶部边界表的推进一定会导向一个新的状态
  - 同类型单色瓶聚合 用于紧凑化，属于实际过程的过渡或者规整（聚合规则策略化）
  - 不同类型单色瓶的转移或或者聚合 也会导向新的状态
    - 不同类型的单色瓶：
      - 高度不同的单色瓶
      - 语义不同的单色瓶（正常单色瓶和石膏盘面的单色瓶）
      - 含有持续触发的单色瓶
    
- 每次转移都会执行一次可能引起状态变化的移动组

```c#
public record TubeMoveAvailability(
    int TubeIndex,
    int Color,
    int ExportCount, // 该颜色可从此瓶倒出的数量
    int AcceptCount // 该颜色可被此瓶接收的数量
);

enum MoveGroupKind
{
    AdvanceBoundary,    // 边界推进型
    NormalizeMonochrome,  // 紧凑化 / 规整型 同类型单色瓶聚合
    MergeMonochrome,
    TransformSemantic
}

// 分层探索
static readonly MoveGroupKind[] ExploreOrder =
{
    MoveGroupKind.AdvanceBoundary,
    MoveGroupKind.MergeMonochrome,
    MoveGroupKind.TransformSemantic,
    MoveGroupKind.NormalizeMonochrome
};

IEnumerable<MoveGroup> GenerateMoveGroups(State state)
{
    var availability = ScanAvailability(state);
    var (froms, tos) = Partition(availability);

    var result = new List<MoveGroup>();

    foreach (var kind in ExploreOrder)
    {
        var groups = ExploreLayer(kind, state, froms, tos);
        result.AddRange(groups);
    }

    return result;
}

IEnumerable<MoveGroup> ExploreLayer(
    MoveGroupKind kind,
    State state,
    IEnumerable<FromTube> froms,
    IEnumerable<ToTube> tos)
{
    foreach (var from in froms)
    {
        var toCandidates = ExtractToTubes(kind, from, tos);

        if (TryGetMoveGroup(kind, from, toCandidates, out var group))
        {
            yield return group;
        }
    }
}
```
```text 
State
  ↓
Explore → MoveGroup（只含真实状态变化） 这里涉及到MoveGroup的类型，优先级和分层探索
  ↓
Apply
  ↓
Normalize（聚合等价类）// 同类型单色瓶子的聚合 同类型（高度，道具状态一致）
  ↓
Hash / Visited / Queue   Hash 规则：要有派利(非单色瓶（顺序排序），单色瓶（按照（有无道具，以及(TubeColorAvailability）排序

这个是hash的具体表现
// 如果多个单色瓶（同颜色、同高度、同语义类型）彼此可交换而不影响未来可达性：
把它们按 (Color, Height, Type, ...) 排序
或把它们聚到固定区间（但必须是确定性的）

Normalize 的两种类型（正式定义）
Normalize-1：位置同构规约（Permutation Equivalence）

本质：位置造成的状态同构

处理层级：Hash / Key

是否改变 State：❌ 不改变

是否产生边：❌ 不是 MoveGroup

作用：判重 / 去对称

✔ 只在 Key 层处理
✔ 不进入搜索语义

Normalize-2：同类型单色瓶聚合等价

本质：同类型单色瓶之间的“结构等价态”

处理层级：入队前 State 规约

是否改变 State：⚠️ 改变 State 表示，但不改变状态语义

是否产生边：❌ 不是 MoveGroup

作用：减少状态数量 / 稳定搜索

✔ 在 Apply 后、入队前执行
✔ 改变的是“状态表示”，不是“问题状态”

State (真实物理状态)
   ↓
Explore → MoveGroup（只是真实状态变化）
   ↓
Apply → NewState
   ↓
Normalize-2（同类型单色瓶聚合）
   ↓
BuildHashKey（包含 Normalize-1 的位置规约）
   ↓
Visited / Enqueue
非常重要的一点：
👉 HashKey 的计算输入，永远是 Normalize-2 之后的 State

你已经明确说了这一句，我再帮你强调一次。

```
```text 
✅ Normalize-2（允许）

所有瓶子 类型相同

所有瓶子 颜色相同

所有瓶子 单色

不发生颜色跨瓶迁移

只是把多个“同构瓶”映射到一个统一结构布局

改变的是：

表示方式

容器编号 / 分布形式

不改变的是：

每个颜色的总体分布能力

后续可达 MoveGroup 集合

❌ MergeMonochrome（必须是 MoveGroup）

满足任意一条即判为 Merge：

不同类型的单色瓶

聚合导致某瓶高度发生变化

聚合前后可用 MoveGroup 集合不同

聚合引发新的边界推进 / 语义转变
```

```text
不，不能是分析最终的结果边，应为只知道边，但并不清楚边的由来
应该是把分组给他

1. 最原始的分组，非单色瓶， 单色瓶
2. 移动能力 { tube_idnex, export_color, export_count, accect_count, accect_color}

这样能不能倒出和接受，通过 export_count， accect_color 可表现出来
可以倒出什么颜色和接受什么颜色：export_color, 已经 accect_color 表现出来

1. 移动能力经过 道具拓展影响 =》 新的移动能力

2. 分组： from组， 以及 to组

3. 如何 根据from组以及to组生成移动移动组，这个有外部注册的 IMoveGroupExplorer 产生
当然这个有默认实现（如果外部不注入的话） 


移动能力
public record TubeMoveAvailability(
    int TubeIndex,
    int ExportColor,
    int ExportCount, // 该颜色可从此瓶倒出的数量
    int AcceptCount, // 该颜色可被此瓶接收的数量
    int AcceptColor,
    TubeStructuralKey StructuralKey,
    Object? extra
);

record TubeStructuralKey(
    int Capacity,
    bool IsMonochrome,
    TubeKind Kind // Normal / Mono / SpecialMono / Fixed / ...
);
 
// 瓶子道具的， 瓶子类型， 哪种拥有关卡级别的影响规则的，就应该是瓶子类型

enum TubeKink
{

}

// 不好
enum TubeEffect  瓶子效果
{

}

enum SlotEffect 格子效果

好：
enum EffectTargetKink
{
    Tube,
    Slot
}

5️⃣ 道具作用的位置（Scope / Position）

这是 Target 的实例化。

正确建模方式
record EffectScope {
    EffectTargetKind TargetKind;
    int TubeIndex;
    int? SlotHeight; // 仅当 TargetKind == Slot
}

interface IEffect {
    EffectTargetKind TargetKind { get; }
    EffectPhase Phase { get; }

    void Apply(
        EffectScope scope,
        CapabilityBuilder builder,
        EffectContext ctx
    );
}

Effect 做的事只有一件：

把“上下文 + 结构 + 规则”，折算成 Capability 的变化


二、TubeKind 的第一个注入点：基础能力生成
📍 注入位置

👉 BaseCapabilityProvider / ComputeBaseAvailability

这是你系统中唯一一个“可以理解 TubeKind 语义”的地方。

能力流水线回顾
TubeKind + TubeState
        ↓
BaseCapabilityProvider
        ↓
TubeMoveAvailability (base)
        ↓
[道具 Effect 修改]

// 能力来源
enum CapabilitySource {
    BaseRule,
    PowerUp,
    TemporaryEffect
}

IAvailabilityProvider（基础 + 道具）

IAvailabilityTransformer（道具叠加）

IMoveGroupExplorer（组合 from / to）

而 Core 只需要做一件事：

把 availability 流水线跑完，然后交出去


```

```c# 
TubeMoveAvailability BuildBaseAvailability(Tube tube, TubeState state)
{
    var remainingCapacity = tube.Capacity - state.FilledCount;

    int exportColor = state.TopColor;
    int exportCount = state.TopColorCount;

    int acceptColor = exportColor;
    int acceptCount = remainingCapacity;

    // 👇 唯一允许判断 TubeKind 的地方
    switch (tube.Kind)
    {
        case TubeKind.Fixed:
            exportCount = 0;
            acceptCount = 0;
            break;

        case TubeKind.Mono:
            acceptColor = tube.FixedColor;
            acceptCount = remainingCapacity;
            break;
    }

    return new TubeMoveAvailability(
        tube.Index,
        exportColor,
        exportCount,
        acceptCount,
        acceptColor,
        extra: null
    );
}
```

```text 
真正还有优化空间的维度只剩 4 类：

A. 可证明的弱同构折叠（不依赖完全等价）
B. 状态表示的“信息最小化 + 延迟展开”  不要把边全部展开，使用迭代器，只计算必要的那次计算
C. 边的生命周期管理（边不是一次性产物）
D. 搜索过程的“形态控制”（不是剪枝）


```
### 道具关卡生成的启发式探索




### TBS下降序列的展开 => 具体移动序列


### 出题模式的探索