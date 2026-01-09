在 有道具 / 不等长 的水排序中，
真正还有优化空间的维度只剩 4 类：

A. 可证明的弱同构折叠（不依赖完全等价）
B. 状态表示的“信息最小化 + 延迟展开”
C. 边的生命周期管理（边不是一次性产物）
D. 搜索过程的“形态控制”（不是剪枝）

三类你还能继续折叠的同构（重点）
（A）未来不可区分同构（Forward-indistinguishable）

定义（非常关键）：

如果两个状态 S1 / S2
在所有未来合法移动序列下：

是否可解

最短解深度

无解性
完全一致

那么它们在搜索中是等价的。

这类同构：

不要求当前结构一致

只要求 未来空间一致

工程化落点

你可以引入：

StateSignatureFuture


例如只包含：

各颜色剩余总量

各颜色“可被消化”的容量上界

空瓶等价计数（按 TubeKind 分组）

👉 用于 visited 的“二级 key”

（B）对称但受限的 Tube 置换

你现在可能已经做了一部分，但还有空间：

相同 Kind

相同 Capacity

相同 FixedColor（若有）

相同“未来可达性约束”

👉 在这个前提下，Tube index 仍然是可交换的

⚠️ 注意：

这不是 Normalize-1

而是 条件化置换群

实践建议

把 Tube 分成 Orbit：

Orbit = (Kind, Capacity, ExtraConstraintSignature)


只在 orbit 内排序 signature。

（C）单调信息擦除（Monotonic Forgetting）

这是你还没明确命名，但其实已经在用的思想。

例子：

某道具一旦触发 → 状态只会更“差”

某种约束一旦出现 → 永远不会解除

那么：

过去的精细信息可以被擦除

比如：

“是怎么被固定的” → 不重要

“是哪个路径触发的” → 不重要

👉 这允许你 合并历史不同、未来相同的状态

3️⃣ ⚠️ 同构折叠的红线（一定要记住）

❌ 不能折叠：

未来可达性不同

可用边集合不同

目标判定不同

你的折叠必须满足：

折叠前后，搜索图是双模拟（bisimulation）的

不是同构，但行为一致。


二、优化计算与存储成本（不是“更省”，而是“不算”）

你现在已经在“省”，但还能继续往 “不算” 推。

1️⃣ 状态表示：两层模型（你现在是 1.5 层）
建议模型
State =
CoreState      // 参与 hash / visited / goal
DeferredState  // 仅在展开边时才需要

CoreState 只包含：

TopBoundary / Boundary-like 抽象

TubeKind + Capacity + 必要标志位

道具“阶段标志”（而非完整信息）

👉 90% 的状态无需完整数据

2️⃣ Apply 的延迟与局部化

你现在可能是：

Apply(move) → 新 State


可以进化为：

ApplyLite(move) → Patch
Materialize(State, Patch)（仅在必要时）


这能带来两个收益：

DFS 中回滚成本极低

BFS 中拷贝成本极低

3️⃣ Hash 的层级化（极重要）

不要只用一个 hash。

H0: Core invariant hash（极快）
H1: Canonical structure hash
H2: Full state hash（最慢）


visited 策略：

H0 冲突 → 再算 H1

H1 冲突 → 再算 H2

👉 大多数节点停在 H0


三、边的“长生”：边不是一次性生成的

这是你第三点里最有潜力、但最容易被忽视的地方。

1️⃣ 边的分类（你已经开始，但还能系统化）

建议你明确三类边：

（A）必然推进边（Monotone Edge）

顶部边界下降

不依赖选择

无副作用

👉 优先级最高，可缓存

（B）结构性边（Structural Edge）

单色瓶 → 非单色

腾出空瓶

不引入新颜色关系

👉 可缓存模板 + 参数化

（C）选择性边（Choice Edge）

多目标选择

强分支

👉 最后生成，且可限额

2️⃣ 边模板化（Edge Template）

不要每次“生成边对象”。

而是：

EdgeTemplate + Binding


例如：

模板：MoveTopColor(from, to)

Binding：(i, j, k)

这允许：

快速枚举

快速剪枝

快速排序

3️⃣ 边的“生命周期”

这是关键思想：

边可以跨节点复用

典型场景：

TopBoundary 相同

Tube orbit 相同

道具阶段相同

👉 你可以缓存：

(StateSignature → EdgeSet)


⚠️ 注意：

这是边集缓存，不是状态缓存

命中率会非常高

四、最后一个你没明说、但最关键的维度
🔥 搜索“形态控制”（不是优化，是稳定性）

你现在的 1–2s 很可能来自：

DFS 深入过早

BFS 宽度爆炸

你可以引入：

1️⃣ 分阶段搜索
Phase 1: 只允许 Monotone + Structural
Phase 2: 引入 Choice Edge

2️⃣ 失败态的提前判定

不是剪枝，而是：

颜色空间不足

必需空瓶数 > 实际空瓶数

某颜色被永久封死

👉 这是“静态无解证据”，不是搜索结果。