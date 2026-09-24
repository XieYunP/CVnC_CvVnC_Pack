# ZH CVnC/CvVnC Phonemizer & ZH YUE CVnC/CvVnC Phonemizer
## 音素化器技术文档

---

## 一、项目概览

本插件为 OpenUtau 提供两个音素化器：

| # | 类名 | Tag | 语言 |
|---|------|-----|------|
| 1 | `ZHCVnCCvVnCPhonemizer` | `ZH CVnC/CvVnC` | ZH |
| 2 | `ZHYUECVnCCvVnCPhonemizer` | `ZH YUE CVnC/CvVnC` | ZH-YUE |

两者共享同一套音素分配逻辑，唯一区别：

- **普通话版**：`Romanize` 使用 `BaseChinesePhonemizer.Romanize`（汉字 → 无声调拼音）
- **粤语版**：`Romanize` 使用 `Pinyin.Jyutping.Instance.HanziToPinyin`（汉字 → 无声调粤拼）

**适用声库**：CVnC / CvVnC 拆音方案（VCCV 风格，含介母、韵尾衔接部）

**依赖**：

- `OpenUtau.dll` / `OpenUtau.Core.dll` / `OpenUtau.Plugin.Builtin.dll`
- `Serilog.dll`
- `Pinyin.dll`（仅粤语版需要 Jyutping）

---

## 二、文件组成

**普通话版**：

- `ZH_CVnC_CvVnC_Phonemizer.cs`
- 目标框架：`net8.0-windows`

**粤语版**：

- `ZH_YUE_CVnC_CvVnC_Phonemizer.cs`
- 目标框架：`net8.0-windows`

**项目文件 (.csproj) 参考**：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="OpenUtau">
      <HintPath>...\OpenUtau.dll</HintPath>
    </Reference>
    <Reference Include="OpenUtau.Core">
      <HintPath>...\OpenUtau.Core.dll</HintPath>
    </Reference>
    <Reference Include="OpenUtau.Plugin.Builtin">
      <HintPath>...\OpenUtau.Plugin.Builtin.dll</HintPath>
    </Reference>
    <Reference Include="Serilog">
      <HintPath>...\Serilog.dll</HintPath>
    </Reference>
    <!-- 仅粤语版需要 -->
    <Reference Include="Pinyin">
      <HintPath>...\Pinyin.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```

**安装**：将编译好的 `.dll` 复制到 `OpenUtau安装目录\Plugins\` 后重启。

---

## 三、scheme.ini 规则文件格式

**位置优先级**：

1. 声库目录 `\<singer.Location>\scheme.ini`
2. 插件目录 `\scheme.ini`

**每一行格式**：

```
pinyin_or_jyutping = Prefix, Main, "Transition1,Transition2,...", Ending
```

**字段说明**：

| 字段 | 说明 |
|------|------|
| `Prefix` | 开头/连接辅音（如 `b`、`ch`、`x`），可为空 |
| `Main` | 主音素（CV 或元音部分，如 `bA`、`xi_`、`la`） |
| `Transition` | 过渡音素，用逗号分隔，可多个，用双引号包裹，可为空 |
| `Ending` | 尾音（如 `u~`、`n~`、`ng~`），可为空 |

**命名后缀约定**：

| 命名 | 性质 | 示例 |
|------|------|------|
| 以 `_` 结尾 | 介母衔接部（拉伸） | `ie'_`、`uo_`、`iA_` |
| 以 `_` 开头 | 韵尾衔接部（固定） | `_Au`、`_e'n`、`_ing` |
| 不以 `_` 结尾的 Main | 拉伸（元音主体） | `bA`、`la` |
| 以 `_` 结尾的 Main | 固定 | `su_`、`xi_`、`ti_` |

**示例行**：

```ini
xian   = x, xi_, "ie'_,_e'n", n~
suo    = s, su_, "uo_", o
bao    = b, bA, "_Au", u~
la     = l, la, "", a
ni     = n, ni, "", i
```

---

## 四、核心概念

### 4.1 音符组 (Note[] notes)

OpenUtau 会把一个主音符以及它后面连续的 `+` 延音合并成一个 `Note[]` 数组传给 `Process`：

- `notes[0]`：主音符（含真实歌词）
- `notes[1..]`：所有 `+` 延音
- `totalDuration = sum(notes[i].duration)`：整个音符合并后的总时长

因此代码里不需要单独处理 `+`，只把 `totalDuration` 当作整体。

### 4.2 固定音素 vs 拉伸音素

**固定音素**：

- 长度锁死在 oto 物理值上，不随 `totalDuration` 变化
- 例：`_Au`、`_e'n`、`_ing`、`n~ R`、以 `_` 结尾的 Main（`su_`、`xi_`）

**拉伸音素**：

- 长度 = 上下两个固定锚点之间的空隙，自动填充
- 例：`bA`、`la`、`ie'_`、`uo_`、`iA_`

### 4.3 oto 字段

`UOto` 包含：

| 字段 | 说明 |
|------|------|
| `Offset` | 音频起点偏移 |
| `Consonant` | 辅音时长（从音素起点到元音开始） |
| `Cutoff` | 右边界（负值，从音频末端往前数的距离） |
| `Preutter` | 预发声时长 |
| `Overlap` | 交叉淡入点 |

**关键派生量**：

- `voiceLen = (-Cutoff) - Preutter` — 预发声到右边界长度
- `consonant = Consonant` — 辅音物理长度

---

## 五、音素分配算法

### 5.1 整体流程

```
Process():
  1. 读取主音符、attr0、attr1
  2. totalDuration = sum(notes[i].duration)
  3. 查 scheme.ini 得到 currentRule
  4. hasPrev / hasNext 判定
  5. 开头音 → ProcessOnset
     非开头 → ProcessContinuation
  6. ProcessTransitions 放置过渡音素
  7. 最后一个音 → ProcessEnding 加尾音到静音
```

### 5.2 开头音 ProcessOnset

优先级：

1. **尝试 `- Prefix`（VC 音素）**
   - `VC 长度 = avgMs = (vcVoice + mainVoice) / 2`
     - `vcVoice = -Cutoff - Preutter`（VC 自身的预发声到右边界）
     - `mainVoice = -Cutoff - Preutter`（Main 的预发声到右边界）
   - VC 不超过自身真实长度 `vcVoice`
   - `VC × consonantStretchRatio`（用户可调）
   - 下限 20 tick
   - → 加 `VC(position = -vcLength)` + `Main(position = 0)`

2. **尝试 `- Main`（整音开头）**
   - → 只加一个音素 `(position = 0)`，不再加 Main

3. **回退**：只加 `Main(position = 0)`

### 5.3 非开头音 ProcessContinuation

**有 Prefix**：

1. **ncv 优先**：`"prevEnding Main"`（如 `"n~ zA"`）
   - 若命中，只加这一个音素 `(position = 0)`，取代 VC + Main，直接 `return`
2. **候选链**：`"prevEnding Prefix"` → `"Prefix"` → `"Main"`
   - `VC 长度 = ComputeVcLength()`（基于 Main 的 Preutter）
   - 平均法：`vcLength = (vcLength + prevEndVoice - prevOverlap) * 2/3`
   - 上限：`prevSpan / 3`（`prevSpan = max(前音符 duration, 时间跨度)`）
   - 空隙避让：若 `gap > 0`，`vcLength = min(vcLength, gap)`
   - 下限：10 tick
   - → 加 `VC(position = -vcLength)` + `Main(position = 0)`

**无 Prefix**：

- 候选链：`"prevEnding Main"` → `"Main"`
- → 加连接别名或 `Main(position = 0)`

### 5.4 过渡音素 ProcessTransitions

步骤：

1. 收集有效的过渡音素（必须存在于声库）
2. 计算 `endingReserve`（尾音预留空间）
3. 计算 `mainRaw`：
   - **若 Main 以 `_` 结尾（固定）**：
     - `a = Preutter(Main)`
     - `b = Preutter(下一个介母衔接部)`
     - `mo = min(Overlap(介母), b)` — 溢出红线的不算
     - `mainRaw = (a + b - mo) / 2`
     - `mainRaw = clamp(mainRaw, max(20, T/10), T/3)`
   - **若 Main 不以 `_` 结尾（拉伸）**：
     - `mainRaw = GetVoiceLength(Main)` — 用于总和计算，不固定位置
4. 计算固定部总和 `fixedSum` 和拉伸部总和 `stretchSum`
5. 分配实际时长（两级优先，见 5.5）
6. 从右往左布局：先放 ending，再倒序放固定音素
7. 拉伸音素自动落在相邻锚点之间

### 5.5 缩短/拉长的分配策略

设：

- `fixedSum` = 所有固定音素的原生时长之和
- `stretchSum` = 所有拉伸音素的原生时长之和
- `minVowelSpace = max(60, totalDuration / 5)`

**分支 1：`totalDuration >= fixedSum + minVowelSpace`（空间充足）**

- 固定音素保持原生时长
- 介母（拉伸）按期望值 `Preutter(自身)` 分配
- main（拉伸）吸收剩余
- → 元音自由伸缩，辅音不动

**分支 2：`totalDuration < fixedSum + minVowelSpace`（空间紧张）**

- 所有音素（含固定）按 `totalDuration / (fixedSum + stretchSum)` 等比缩
- → 元音、辅音一起缩，避免单一音素挤压其它

### 5.6 尾音到静音 ProcessEnding

- **条件**：无下一个音符 且 `rule.Ending` 非空
- **音素**：`"Ending R"` 优先，找不到用 `"Ending -"`
- **长度**：`min(totalDuration / 6, 60)`
- **位置**：`totalDuration - endingLength`

### 5.7 介母布局

- 介母（以 `_` 结尾的过渡音素）起点 = `mainRaw`
- 即紧贴 main 右边缘，不再从右边缘往回推
- 若越过下一锚点，限制到 `nextAnchor - 10`

### 5.8 main 保底

- `minMain = max(20, totalDuration / 10)`
- `mainRaw` 计算结果 clamp 到 `[minMain, totalDuration / 3]`
- 从右往左布局时 `cursor` 不许低于 `minMain`
- 防止韵尾 / ending 吞掉整音

---

## 六、G2P（汉字转拼音）

### 6.1 触发时机

OpenUtau 在渲染前调用 `SetUp()` 一次：

1. 收集所有音符的主歌词
2. 调用 `Romanize()` 批量转换
3. 通过 `ChangeLyric()` 把拼音写回 `lyrics`

之后 `Process()` 拿到的 `lyric` 已是拼音。

### 6.2 普通话版

```csharp
protected virtual string[] Romanize(IEnumerable<string> lyrics)
{
    return BaseChinesePhonemizer.Romanize(lyrics);
}
```

- 使用 `OpenUtau.Core` 内置字典
- 输出无声调拼音（如 `"ni"`、`"hao"`）
- 支持简体 + 繁体汉字

### 6.3 粤语版

```csharp
protected virtual string[] Romanize(IEnumerable<string> lyrics)
{
    return Pinyin.Jyutping.Instance.HanziToPinyin(
        lyrics.ToList(),
        Pinyin.CanTone.Style.NORMAL,
        Pinyin.Error.Default
    ).Select(res => res.pinyin).ToArray();
}
```

- 使用 Pinyin 库的 Jyutping 转换器
- 输出无声调粤拼（如 `"nei"`、`"hou"`）
- 支持简体 + 繁体汉字

### 6.4 注意事项

- `scheme.ini` 的键必须与 G2P 输出一致（无声调）
- 若 G2P 抛异常，音素器会回退到把汉字当音素名直接发送

---

## 七、参数与属性

### 7.1 音符属性 attr0 / attr1

从 `note.phonemeAttributes` 读取，`index` 区分：

- `index 0` → 主音素（`voiceColor`、`toneShift`、`alternate`）
- `index 1` → 辅音（`consonantStretchRatio`、`voiceColor`）

**字段**：

| 字段 | 说明 |
|------|------|
| `voiceColor` | 音色后缀（如 `"Soft"`），会附加到音素名 |
| `toneShift` | 音高偏移 |
| `alternate` | 替代音源编号 |
| `consonantStretchRatio` | 辅音拉伸倍率（仅对 VC 开头音生效） |

### 7.2 音色后缀 AppendVoiceColor

规则：

- 若音素名已以 `" <color>"` 结尾，不重复添加
- 否则追加 `" <color>"`

---

## 八、常见问题排查

**[Q1] 输入汉字没反应**

A：检查是否有 `using OpenUtau.Core;`（普通话）或 `using Pinyin;`（粤语），`scheme.ini` 键是否为无声调。

**[Q2] 音素器不显示在列表**

A：

- 检查 `[Phonemizer]` 特性 Tag 是否唯一
- DLL 是否放在 `Plugins\` 目录
- 目标框架是否与 OpenUtau 运行时一致

**[Q3] 辅音太长挤掉元音**

A：检查 `ProcessContinuation` 里的碰撞检测是否生效，`ComputeVcLength` 里不应有 `totalDuration / 3` 上限。

**[Q4] 短音时辅音听不清**

A：确认使用了两级优先策略（方案 C）。极限短音下辅音会被压缩，这是不可避免的。

**[Q5] 长音时介母被拉伸过长**

A：介母起点紧贴 main 右边缘（= `mainRaw`），不再从右边缘回推。`mainRaw` 使用 `(Preutter(main) + Preutter(med) - min(Overlap, Preutter)) / 2`。

**[Q6] `+` 连音被当独立音符**

A：OpenUtau 已自动合并入 `notes[]`，代码里 `totalDuration` 就是合并后的总时长，无需额外处理。若 `lyric == "+"` 出现在 `notes[0]`，说明是孤立 `+`，建议返回 `MakeSimpleResult("-", attr0)`。

---

## 九、开发历程与关键决策

**阶段 1：初版**

- 失败：`SetUp` 里手动合并 `+` 到前音符，与 OpenUtau 架构冲突
- 失败：空音符数组返回导致渲染崩溃

**阶段 2：重构**

- 删除 `SetUp` 里的合并逻辑，改为信任 `notes[]` 数组
- 孤立 `+` 返回静音占位符

**阶段 3：拉伸/固定分离**

- 引入以 `_` 结尾 / 开头判断
- 从右往左布局固定音素

**阶段 4：oto 参考字段迭代**

- 试过 `Consonant`、`Preutter`、`-Cutoff-Preutter`、平均值、`max(Preutter, Overlap)`
- 最终：main 用 `(Preutter(main) + Preutter(med) - min(Overlap, Preutter(med))) / 2`
- 韵尾用 `Preutter`
- 介母用 `Preutter`

**阶段 5：缩短策略迭代**

- 失败 1：全部等比缩 → 辅音听不清
- 失败 2：拉伸先缩到 20% → 元音被压没
- 失败 3：`T >= fixedSum` 判定 → 韵尾挤压 main
- 最终：`T >= fixedSum + minVowelSpace` 才走"固定不动"，否则所有音素整体等比缩

**阶段 6：长度限制**

- 开头 VC 不超过自身真实长度，且与 main 取平均
- 中间 VC 有平均法 + `prevSpan/3` 上限 + 空隙避让 + 下限 10
- main 有 `minMain = max(20, T/10)` 保底

---

## 十、代码结构导航

**类**：

- `ZHCVnCCvVnCPhonemizer`（普通话版）
- `ZHYUECVnCCvVnCPhonemizer`（粤语版，结构完全相同）

以下是两者共有的成员（粤语版多一个 Pinyin 引用）：

| 成员 | 说明 |
|------|------|
| `SetSinger()` | 加载 `scheme.ini` |
| `LoadSchemeFile()` | 解析规则文件 |
| `ParseRuleString()` | 处理带引号的规则串 |
| `Romanize()` | [虚拟] G2P 转换 |
| `ChangeLyric()` | 写回拼音 |
| `SetUp()` | 批量 G2P 入口 |
| `Process()` | 主入口 |
| `ProcessOnset()` | 开头音 |
| `ProcessContinuation()` | 非开头音 |
| `ComputeVcLength()` | VC 长度计算 |
| `GetVoiceLength()` | `-Cutoff - Preutter` |
| `GetFixedDuration()` | `(voiceLen + consonant) / 2` |
| `ResolveAlias()` | 音素别名解析 |
| `ProcessTransitions()` | 过渡音素布局 |
| `ProcessEnding()` | 尾音到静音 |
| `GetAttr()` | 读取音符属性 |
| `AddMainPhoneme()` | 添加主音素 |
| `CheckOtoUntilHit()` | 依次尝试候选别名 |
| `CheckOtoExists()` | 单音素存在性检查 |
| `AppendVoiceColor()` | 音色后缀 |
| `MakeSimpleResult()` | 快捷返回 |
| `CVnCRule` | 规则数据结构 |

**字段**：

| 字段 | 说明 |
|------|------|
| `singer` | `USinger` 实例 |
| `rules` | `Dictionary<string, CVnCRule>` |

---

*文档结束*