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

**目标框架**：

- net8 版：`net8.0-windows`（兼容旧版 OpenUtau）
- net10 版：`net10.0-windows`（兼容新版 OpenUtau）

---

## 二、文件组成

**普通话版**：

- `ZH_CVnC_CvVnC_Phonemizer.cs`
- `ZH_CVnC_CvVnC_Phonemizer.csproj`（net8）
- `ZH_CVnC_CvVnC_Phonemizer.net10.csproj`（net10）

**粤语版**：

- `ZH_YUE_CVnC_CvVnC_Phonemizer.cs`
- `ZH_YUE_CVnC_CvVnC_Phonemizer.csproj`（net8）
- `ZH_YUE_CVnC_CvVnC_Phonemizer.net10.csproj`（net10）

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

**重要**：编译产物在 `bin\Debug\<框架>\`，**不是** `obj\...\ref\` 或 `obj\...\refint\` 里的。后者是引用程序集，OpenUtau 加载后会无响应。

**注意**：net8 版与 net10 版的 `[Phonemizer]` Tag 相同，**同一个 OpenUtau 的 `Plugins\` 目录里只能放一个版本**，否则会冲突。

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
| `Consonant` | 辅音物理长度 |
| `Cutoff` | 右边界（负值，从音频末端往前数的距离） |
| `Preutter` | 预发声时长（辅音起点到元音起点） |
| `Overlap` | 交叉淡入点 |

**关键派生量**：

- `voiceLen = (-Cutoff) - Preutter` — 预发声到右边界长度
- `consonant = Consonant` — 辅音物理长度

### 4.4 toneShift 类型差异

| OpenUtau 版本 | `PhonemeAttributes.toneShift` 类型 | 写法 |
|---|---|---|
| net8 | `int` | `note.tone + attr.toneShift` |
| net10 | `int?` | `note.tone + (attr.toneShift ?? 0)` |

同一份源码不能同时用于两个版本。net8 与 net10 项目需各自维护对应的 `.cs` 文件。

本项目维护 4 份源码：ZH net8 / ZH net10 / YUE net8 / YUE net10。改逻辑时四份都要同步，唯一区别是 `toneShift` 写法。

### 4.5 真实物理时长（毫秒）感知
由于不同曲速（BPM）下，相同的 Tick 代表的时间不同。为了精准判断“极短音”，本插件全面引入了真实物理时长判定：
`durationMs = timeAxis.TickPosToMsPos(endTick) - timeAxis.TickPosToMsPos(note.position)`
核心的剔除与限幅逻辑全部基于毫秒进行。

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

**1. 尝试 `- Prefix`（VC 音素）**

- `vcVoiceMs = -Cutoff(vc) - Preutter(vc)`，下限 60 ms
- 读 main 的 `mainPre = Preutter(main)`（< 0 截断为 0）、`O = Overlap(main)`（可负）
- `mainOvAbs = |O|`
- `mainConsPart = max(0, mainPre - mainOvAbs)`
- VC 视觉长度：
      vcLengthMs = mainConsPart + (vcVoiceMs + mainOvAbs) / 2
  乘 `attr1.consonantStretchRatio`，下限 20 ms

**位置（按 O 正负分两支）**

O ≥ 0（正 Overlap）：
- `mainPos = 0`
- `vcPos = -vcLength + MsToTick(O)`

O < 0（负 Overlap，按 |O| : mainPre 分摊）：

      denom = mainPre + mainOvAbs
      if denom > 0:
          mainShift = |O| * mainPre / denom
          vcShift   = |O| * |O|    / denom
          mainPos   = MsToTick(mainShift)
          vcPos     = -vcLength + MsToTick(vcShift)
      else:
          mainPos = 0
          vcPos   = -vcLength

→ 加 `VC(position = vcPos)` + `Main(position = mainPos)`，`return`

**2. 尝试 `- Main`（整音开头）**：只加一个音素 `(position = 0)`

**3. 回退**：只加 `Main(position = 0)`

**行为表**

| mainPre | \|O\| | mainShift | vcShift | 空隙 |
|---|---|---|---|---|
| 100 | 0（正 O） | 0 | 0 | 0 |
| 100 | 50 | 33 | 17 | 16 |
| 100 | 100 | 50 | 50 | **0** |
| 100 | 200 | 67 | 133 | **-66（VC 越过 Main，数据异常）** |

**为什么这样分摊**：负 Overlap 意味着音频上 VC 尾部与 Main 头部该有一段共享。如果 Main 起点完全不动、只把 VC 右边界缩到 0，会出现大段空隙；如果把 Main 起点后移到 VC 右边界，会出现双重辅音。按 `|O| : mainPre` 比例让双方各让一步，`|O| = mainPre` 时恰好接上、无空隙也无重叠。

### 5.3 非开头音 ProcessContinuation

**1. 极端短音秒级剔除**
- 若当前音符物理时长 `durationMs < 90ms`，强制剔除所有前置 VC（直接退回 NCV 或 Main），防止极短音越界。

**2. VC 限制（智能上限与向后预测）**
- **主音负重叠（Overlap）钳制**：如果 Overlap 为负值，最大允许的向左延伸幅度为前一个音符物理时长的 1/4（至少 20ms，至多 60ms），超出部分温和软削减半。
- **长度上限**：
  - `maxVcByPrevMs` = `max(20, prevDurMs * 0.4)`
  - `maxVcBySelfMs` = `max(25, durationMs * 0.5)`
  - 如果 VC 超限，取平均值软削。
- **死线防越界**：通过 `vcPosition < -maxVcLength` 和 `pn.duration * 0.4` 等硬防线，确保红线绝不深入前一个音符的元音区。

### 5.4 ComputeVcLength（VC 视觉长度）

```csharp
int vcVoice = -Cutoff(vc) - Preutter(vc);  // VC 自身辅音段
int ovAbs   = |Overlap(main)|;             // main 与 VC 的共享交叉段
int totalMs = vcVoice + ovAbs / 2;         // 剔除多余的 pre，防止暴涨
return max(30, MsToTick(totalMs));

### 5.5 ProcessTransitions 主逻辑

**步骤 0：前向智能让位**
- 检查 `nextNeighbour`。如果下一个音符有 Prefix，且本身不是极短音（>=60ms），则从当前音符扣除 `min(estVcMs, 50, durationMs*0.3)` 的空间作为 `effectiveTotalDuration`，主动为下一个 VC 预留空间。

**步骤 1：毫秒级过渡音分级剔除**
- `< 60ms`：剔除所有非介母（即剔除韵尾）。
- `< 40ms`：剔除所有介母，只留主音。

**步骤 2：尾音预留**
- 若 `effectiveTotalDuration <= 45`，不预留。
- 若为最后一个音符，取 `(v+c)/2` 并限制在 `effectiveTotalDuration/6` 与 `60` 之间。

**步骤 3：main 原始长度与固定音限制**
- **固定音（以 `_` 结尾）**：限制其最长不超过 `effectiveTotalDuration / 2`，防止过长霸屏。
- **拉伸音**：直接使用 `GetVoiceLength`。

**步骤 4 & 5：拆辅音 / 元音（平均法回归）**
- **Main**：正 Overlap 平分重叠段，负 Overlap 平分延伸段。
- **介母**：使用加权平均法 `expected = (pre * 2 + ov) / 3`，然后再用 `transDurationMs * 0.2`（中短音）进行温和软削。

**步骤 6 & 7：全局等比缩放与固定音防膨胀**
- 计算 `scale = min(1.5, totalDuration / totalRaw)`。
- **核心修复**：如果 `mainIsFixed == true` 且 `scale > 1.0`，强制该主音的辅音和元音不参与放大，保持原长，多出的空间留给介母或尾部。
- 若 `effectiveTotalDuration <= 60`，辅音不得大于元音；若 `> 60`，辅音最多占 3/4。

**步骤 8 & 8b：终极防溢出与整数对齐**
- 在 double 精度下优先砍尾音、过渡音，最后砍元音。
- 取整后计算 `diff`，若超了则依次削减，若多出则直接空着（作为为下一个音符预留的空间）。

**步骤 9：布局**
- 韵尾从右往左排（紧贴 `totalDuration - actualEnding`）。
- 介母紧贴主音结束点，若靠太近则强行推开 4 tick。

### 5.6 关键设计点
**双向预测机制**：前向让位（主动腾挪空间）+ 向后限制（防御暴力侵蚀），在不具备全局上下文的情况下，完美实现了跨音符的视觉与听觉平衡。
**固定音防膨胀**：防止 `gu_` 这种固定音在长音区（scale > 1）被等比放大，导致其拉伸部分挤占介母和韵尾的生存空间。
**毫秒级分级剔除**：无视 BPM 变化，用真实物理时间判断短音，按 `VC -> 韵尾 -> 介母` 的优先级丢弃。

**步骤 7：缩放（元音 1/2 下限 + 剩余按需求比例）**

```
if (S1 + S2 + S3 > totalDuration):
    minVowel = totalDuration / 2       // ★ 元音至少占一半
    actualVowel = min(S1, minVowel)
    vScale = actualVowel / S1

    remaining = totalDuration - actualVowel
    s23 = S2 + S3
    if (remaining >= s23):
        tScale = 1; mScale = 1
    else:
        roomT = remaining * S2 / s23     // ★ 按需求比例
        roomM = remaining - roomT
        tScale = roomT / S2
        mScale = roomM / S3
```

语义：

- **元音永远至少占 `totalDuration / 2`**，但不超过元音实际需求
- 剩下的空间按"衔接段想要多少"和"main 辅音想要多少"的比例分配
- 相当于：**元音优先保一半**，剩下的一半内部按需分配

**步骤 8：计算实际长度**

```csharp
int actualCons = (int)(mainCons * mScale);
int actualVowel = (int)(mainVowel * vScale);

// ★ 辅音视觉上限 = 元音视觉长度的 1.5 倍
int consCap = actualVowel * 3 / 2;
if (consCap < 20) consCap = 20;
if (actualCons > consCap)
{
    int surplus = actualCons - consCap;
    actualCons = consCap;
    actualVowel += surplus;      // 多出的 tick 转给元音
}

int actualMain = actualCons + actualVowel;
```

`/3*2` 这里的 `3/2` 就是"辅音最大是元音的 1.5 倍"。想更严就改成 `1`，想更松就 `2`。

**步骤 9：布局**

- **6a. 从右往左排韵尾（非介母）**：
  - `cursor = T - actualEnding`
  - 倒序遍历，跳过介母，依次 `cursor -= actualTrans[i]`
  - 下限 0，记录 `positions[i]`，`placed[i] = true`
- **6b. 介母紧贴 main 右边缘**：
  - `newPos = actualMain`
  - 下一锚点 = 后面第一个 `placed[j]` 的 `positions[j]`（若无则 `T - actualEnding`）
  - 若 `newPos >= nextAnchor` → `newPos = max(0, nextAnchor - 1)`（**1 tick 间隔**）

**步骤 10：输出诊断日志**

```
[LAYOUT] MAIN=xxx T=... mainRaw=... mainCons=... mainVowel=...
         tailCons=... endingReserve=...
         S1=... S2=... S3=...
         vScale=... tScale=... mScale=...
```

### 5.6 关键设计点

**元音下限 1/2**：短音时优先保证元音占一半。因为元音是听感主体，辅音太长会让音节听起来像干擦。

**衔接 / 辅音按需求比例**：如果某个音节的韵尾本来就很短（S2 小），它不会抢太多空间；如果 main 辅音特别长（S3 大），才被多分。这比写死比例（如 4:6）更智能。

**main 辅音 2/3 上限**：保证 main 元音至少 `mainRaw / 3`，防止某些声库的 `Consonant` 字段过大把元音挤没。

**consCap 软削**：如果缩放后辅音仍超过元音 1.5 倍，多出的 tick 转给元音，进一步保护元音视觉。

**VC 软削**：VC 视觉长度超过自身物理辅音长度或侵占前一个音符 1/3 时，取当前值与上限的平均值（**软削，不硬砍**）。硬砍会让 VC 缩进音符内部，视觉上"消失"。

### 5.7 尾音处理
尾音在 `ProcessTransitions` 的步骤 10b 中统一处理。如果无下一音符且尾音预留空间 `actualEnding > 0`，则尝试 `Ending R`、`Ending -`，将其放置在 `totalDuration - actualEnding` 的位置。

### 5.8 main 下限

- 只有硬下限 `mainRaw = max(mainRaw, 20)`
- 不再随 T 变（取消 `T/10` 和 `T/3` 的比例限制）
- 目的：让固定音素真正"固定"，不被比例撑大

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
| `toneShift` | 音高偏移（net8 是 `int`，net10 是 `int?`） |
| `alternate` | 替代音源编号 |
| `consonantStretchRatio` | 辅音拉伸倍率（仅对 VC 开头音生效） |

### 7.2 音色后缀 AppendVoiceColor

规则：

- 若音素名已以 `" <color>"` 结尾，不重复添加
- 否则追加 `" <color>"`

---

## 八、常见问题排查

### 8.1 使用者常见问题

**[U1] 输入汉字没反应**

A：`scheme.ini` 的键必须与 G2P 输出一致（无声调拼音 / 粤拼）。若声库目录下没有 `scheme.ini`，会回退到插件目录的 `scheme.ini`。

**[U2] 音素器不显示在列表**

A：

- 检查 OpenUtau 的 `Plugins\` 目录，DLL 是否复制进去
- **确认复制的是 `bin\Debug\<框架>\` 里的 DLL，不是 `obj\...\ref\` 或 `refint\` 里的**
- 确认 OpenUtau 版本与 DLL 目标框架匹配（net8 版对 net8 版 OpenUtau，net10 版对 net10 版）
- **同一个 `Plugins\` 目录里不能同时放 net8 和 net10 两个版本**，Tag 相同会冲突

**[U3] 音符被唱出奇怪的效果**

A：先确认 DLL 是当前版本（`bin\Debug\<框架>\` 的时间戳），再检查：

- 短音时元音是否仍被保护（听感上不是干擦）
- 开头音 VC 与 Main 之间有没有大段空隙（负 O 分摊是否生效）
- `scheme.ini` 是否加载成功（日志里搜 `Loaded ... rules`）

### 8.2 开发者常见问题

**[D1] 音符总时长判定异常**

A：OpenUtau 会把主音符和它后面所有 `+` 合并成 `notes[]`，`totalDuration = sum(notes[i].duration)`。

**[D2] net8 编译时提示 `??` 不可套用至 'int'**

A：net8 的 `toneShift` 是 `int`，net10 是 `int?`。全局替换：

- net8 源码：`(attr.toneShift ?? 0)` → `attr.toneShift`
- net10 源码：保持 `(attr.toneShift ?? 0)`

**[D3] 两版本（net8 / net10）行为不一致**

A：源码需各自独立维护。改一处逻辑时，**两份 `.cs` 都要同步改**，尤其注意 `toneShift` 写法差异。

**[D4] 编译通过但音素器加载后无效**

A：

- 检查 `[Phonemizer]` 的 Tag 是否与其他插件重复
- 检查 `class` 是否被 `public` 修饰
- **检查是否复制了正确的 DLL**（`bin\` 里的，不是 `obj\` 里的）

**[D5] 元音被挤压到几乎不可见**

A：按代码逻辑逐层核对：

1. `mainRaw` 是否 ≥ 20
2. `mainCons = min(Consonant, mainRaw * 2/3)` 是否生效
3. `S1 ≥ totalDuration / 2` 时 `vScale < 1` 是否生效
4. `consCap = actualVowel * 3 / 2` 是否触发
5. 该音符 `T` 是否 < 20 tick（物理不可变）

---

## 九、开发历程与关键决策

**阶段 1：工程版本**

探索与试错阶段，验证音素器架构可行性。

- 失败：`SetUp` 里手动合并 `+` 到前音符，与 OpenUtau 架构冲突
- 失败：空音符数组返回导致渲染崩溃
- 重构：删除 `SetUp` 里的合并逻辑，改为信任 `notes[]` 数组
- 引入以 `_` 结尾 / 开头判断，分离"拉伸音素"与"固定音素"
- 从右往左布局固定音素，介母、韵尾、整音分别处理
- oto 字段迭代：试过 `Consonant`、`Preutter`、`-Cutoff-Preutter`、平均值
- 缩短策略迭代：失败过"全部等比缩"、"拉伸先缩 20%"、"T >= fixedSum 判定"
- 长度限制：开头 VC 不超过自身真实长度；中间 VC 有平均法 + `prevSpan/3` 上限 + 空隙避让

**阶段 2：发布版本**

在工程版本基础上重构、修正与扩充，形成稳定发布版。

- **过渡音素布局重构**：拆成 6a（韵尾从右往左排）和 6b（介母紧贴 main 右边缘）
- **main 固定时长定稿**：`mainRaw = (a + b) / 2`，a、b 物理对等
- **时长分配定稿**：`scale = min(1, T / totalNeeded)`，所有音素统一等比缩
- **VC 长度算法定稿**：`VC 长度 = -Cutoff - Preutter`
- **net10 兼容版本**：`toneShift` 类型差异，源码独立维护

**阶段 3：三段式元音保护（当前）**

用户反馈：短音时辅音占满整块，元音被挤压。

- **问题根因**：早期 `mainProtected = mainPre` 导致 main 辅音段先占满，元音只剩残渣
- **修复 1**：`mainCons` 从 `Preutter` 改为 `Consonant`，语义更贴合物理辅音长度
- **修复 2**：加 2/3 上限，保证 main 元音至少 `mainRaw / 3`
- **修复 3**：从二段式（vowel/protected）改为三段式（元音/衔接/main 辅音），各有独立缩放优先级
- **修复 4**：改为元音下限 `T/2` + 剩余按 S2:S3 需求比例分配
- **修复 5**：`consCap = actualVowel * 3 / 2` 软削，多出的 tick 转给元音
- **修复 6**：VC 软削（`vcVoiceMax` 和 `maxOverrun`），不硬砍
- **诊断日志**：`[LAYOUT]` 输出所有关键变量；`[VC]` 输出 VC 关键值

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
| `ComputeVcLength()` | VC 长度计算（含 ovAbs 平均 + mainPre） |
| `GetMainOverlap()` | main Overlap 转 tick |
| `GetVoiceLength()` | `-Cutoff - Preutter` |
| `GetFixedDuration()` | `(voiceLen + consonant) / 2`（`_` 开头用 voiceLen） |
| `ResolveAlias()` | 音素别名解析 |
| `ProcessTransitions()` | 过渡音素布局（三段分类 + 元音下限 + 按需求比例） |
| `ProcessEnding()` | 尾音到静音 |
| `GetAttr()` | 读取音符属性 |
| `AddMainPhoneme()` | 添加主音素 |
| `CheckOtoUntilHit()` | 依次尝试候选别名 |
| `CheckOtoExists()` | 单音素存在性检查 |
| `AppendVoiceColor()` | 音色后缀 |
| `MakeSimpleResult()` | 快捷返回 |
| `CVnCRule` | 规则数据结构 |

**关键常量**（可调）：

| 位置 | 值 | 说明 |
|------|-----|------|
| `ProcessContinuation` | `durationMs < 90` | 极短音剔除 VC 阈值 |
| `ProcessContinuation` | `prevDurMs * 0.4` | VC 侵占前音的最大比例 |
| `ProcessTransitions` | `transDurationMs < 60` | 剔除韵尾阈值 |
| `ProcessTransitions` | `transDurationMs < 40` | 剔除介母阈值 |
| `ProcessTransitions` | `(pre * 2 + ov) / 3` | 介母长度加权平均公式 |
| `ProcessTransitions` | `effectiveTotalDuration / 2` | 固定主音最大长度上限 |
| `ProcessTransitions` | `maxConsAllowed` (0.75) | 长音下辅音最大占比 |
| `ProcessTransitions` | `nextDurMs` 估算 | 前向让位计算预估 VC 长度 |

**字段**：

| 字段 | 说明 |
|------|------|
| `singer` | `USinger` 实例 |
| `rules` | `Dictionary<string, CVnCRule>` |

---

## 十一、调试

本版本内置了非常详细的 `[CVAL]` 实时诊断日志。在 OpenUtau 的 `Logs Location` 中搜索 `[CVAL]` 即可查看。

**日志解读示例：**

=== [CVAL] gua | T:183 (254ms) | 前音(333ms) ===
  【主音】gu_ | Oto:(C:135.0 P:109.0 O:33.0)
  【前置】前导[i g]
  【原始】需求:144 | 主音底长:75 (固:75 元:0)
  【缩放】scale:1.271 | 主音: 95.31 (固:95.31 元:0.00) | 尾音:0.00
  过渡[0]: ua_ (介母) | 计算值:74.34 -> 分配:74 | 坐标:95

**字段说明：**
- **T / ms**：当前音符的 Tick 长度和真实物理长度。
- **前音(XXms)**：前一个音符的物理时长，用于评估 VC 侵占空间。
- **需求 / 底长**：缩放前各音素渴望的空间大小。
- **缩放 / 计算值 -> 分配**：经过全局等比、固定音防膨胀、软削后的最终长度与坐标。

---

*本文档及代码逻辑由 DeepSeek 辅助开发。*