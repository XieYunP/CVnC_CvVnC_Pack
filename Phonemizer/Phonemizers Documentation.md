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
   - `vcVoice = -Cutoff - Preutter`（VC 自身的预发声到右边界）
   - `mainVoice = -Cutoff - Preutter`（Main 的预发声到右边界）
   - `avgMs = (vcVoice + mainVoice) / 2`，但 `avgMs` 不超过 `vcVoice`
   - `vcLength = MsToTick(avgMs) × consonantStretchRatio`（用户可调）
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
   - `vcLength = ComputeVcLength()`（见 5.4）
   - **软上限 1**（VC 自身物理辅音长度）：
     `vcVoiceMax = -Cutoff(vc) - Preutter(vc)`
     若 `vcLength > vcVoiceMax`，`vcLength = (vcLength + vcVoiceMax) / 2`
   - **软上限 2**（不侵占前一个音符超过 1/3）：
     `cap = gap + prevDur / 3 + mainOverlap`（`gap = max(0, noteStart - prevEnd)`，最小 20）
     若 `vcLength > cap`，`vcLength = (vcLength + cap) / 2`
   - 下限：10 tick
   - `vcPosition = -vcLength + mainOverlap`
   - → 加 `VC(position = vcPosition)` + `Main(position = 0)`
   - 输出诊断日志 `[VC]`

**无 Prefix**：

- 候选链：`"prevEnding Main"` → `"Main"`
- → 加连接别名或 `Main(position = 0)`

### 5.4 ComputeVcLength（VC 视觉长度）

```csharp
int vcVoice = -Cutoff(vc) - Preutter(vc);     // VC 自身辅音段
int ovAbs   = |Overlap(main)|;                 // main 与 VC 的共享交叉段
int pre     =  Preutter(main);                 // main 预发声到元音起点

int totalMs = (vcVoice + ovAbs) / 2 + pre;
return max(30, MsToTick(totalMs));
```

语义：VC 视觉宽度 = "VC 辅音的物理长度与 main 交叉段的平均" + "main 预发声到元音起点"。前半段由 VC 决定，后半段由 main 决定，取一个折中值。

**注意**：OpenUtau 引擎自身会把 `Overlap` 段渲染为前音素尾部和当前音素头部的共享段，phonemizer 无需再叠加。

### 5.5 ProcessTransitions 主逻辑

**步骤 1：收集有效过渡音素**

对每个 `rule.TransitionPhonemes`，检查是否在声库中存在（`CheckOtoExists` 或 `CheckOtoUntilHit`）。存在则保留：

- `isMedial = t.EndsWith("_") && !t.StartsWith("_")` → 介母衔接部
- 其余（以 `_` 开头）→ 韵尾衔接部

**步骤 2：尾音预留**

```
endingReserve = 0
if (!hasNext && rule.Ending 非空)
    endingReserve = max(20, min(totalDuration / 6, 60))
```

**步骤 3：mainRaw（main 原始长度）**

- **Main 以 `_` 结尾（固定）**：
  - `a = -Cutoff(main) - Preutter(main)`
  - `b = Preutter(第一个介母衔接部)`
  - `mainRaw = (a + b) / 2`，退化到 a 或 b
- **Main 不以 `_` 结尾（拉伸）**：
  - `mainRaw = -Cutoff(main) - Preutter(main)`
- 下限 `mainRaw = max(20, mainRaw)`

**步骤 4：main 拆辅音 / 元音**

```csharp
int mainCons;
if (oto.Consonant > 0 && oto.Consonant <= mainRaw)
    mainCons = oto.Consonant;
else
    mainCons = (int)(oto.Preutter * 0.7);

// ★ 辅音不超过 mainRaw 的 2/3，保证元音至少 1/3
int maxMainCons = mainRaw * 2 / 3;
if (mainCons > maxMainCons) mainCons = maxMainCons;
if (mainCons < 0) mainCons = 0;

int mainVowel = mainRaw - mainCons;  // 元音段
```

**步骤 5：介母拆辅音 / 元音（同上）**

```csharp
int expected = max(Preutter(med), Overlap(med));
if (expected < 20) expected = 20;

int prot = min(Preutter(med), expected);
if (prot < 20) prot = 20;
if (prot > expected) prot = expected;

// ★ 不超过 expected 的 2/3
int maxProt = expected * 2 / 3;
if (prot > maxProt) prot = maxProt;

medCons[i]  = prot;
medVowel[i] = expected - prot;
```

**步骤 6：三段分类**

```
S1 = 元音段总量 = mainVowel + Σ medVowel
S2 = 衔接段总量 = tailCons + endingReserve + Σ medCons
S3 = main 辅音段 = mainCons
```

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

### 5.7 尾音到静音 ProcessEnding

- **条件**：无下一个音符 且 `rule.Ending` 非空
- **音素**：`"Ending R"` 优先，找不到用 `"Ending -"`
- **长度**：`min(totalDuration / 6, 60)`
- **位置**：`totalDuration - endingLength`

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

A：查 `[LAYOUT]` 和 `[VC]` 日志（OpenUtau `Help → Open Logs Location`）：

- `mainCons` 是否被卡在 2/3 —— 正常
- `S1` 是否至少占 `T/2` —— 是说明元音被保护了
- `vScale` / `tScale` / `mScale` 三个是否合理 —— 都 < 1 说明整体被压
- `[VC]` 里 `vcLength` / `vcPos` 是否落在负数（正常）或正数（异常，VC 侵入音符内部）

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

A：检查顺序：

1. `[LAYOUT]` 里 `mainCons` 是否 = `mainRaw * 2/3`（2/3 上限是否生效）
2. `S1` 是否 ≥ `T/2`（元音下限 1/2 是否生效）
3. `consCap` 是否被触发（辅音视觉是否被软削）
4. 该音符 `T` 是否过小（< 20 tick 时数值受限，物理不可变）

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

**阶段的教训**：

1. **"固定音素"不应是"永不缩"**。短音时真正该优先缩的是元音，其次衔接，最后才是 main 辅音。
2. **软削优于硬砍**。VC 硬砍到小于 `mainOverlap` 时，`vcPosition` 会变成正数，VC 侵入音符内部，视觉上"消失"。
3. **诊断日志是排查的第一生产力**。`[LAYOUT]` 一行能把所有关键变量暴露出来，比肉眼推断快 10 倍。
4. **四文件同步**。改一处逻辑后立刻同步另外三份，只改 `toneShift` 写法。

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
| `ProcessTransitions` `minVowel` | `totalDuration / 2` | 元音下限比例 |
| `ProcessTransitions` `maxMainCons` | `mainRaw * 2 / 3` | main 辅音上限比例 |
| `ProcessTransitions` `maxProt` | `expected * 2 / 3` | 介母辅音上限比例 |
| `ProcessTransitions` `consCap` | `actualVowel * 3 / 2` | 辅音视觉上限（1.5 倍元音） |
| `ProcessTransitions` 6b `nextAnchor` 间隔 | `1` | 介母与下一锚点最小间隔 |
| `ProcessContinuation` `maxOverrun` | `prevDur / 3` | VC 侵占前音符最大比例 |
| `ComputeVcLength` 最小值 | `30` | VC 最小 tick |

**字段**：

| 字段 | 说明 |
|------|------|
| `singer` | `USinger` 实例 |
| `rules` | `Dictionary<string, CVnCRule>` |

---

## 十一、调试指南

### 11.1 开启日志

OpenUtau `Help → Open Logs Location` 打开日志文件，搜 `[LAYOUT]` 和 `[VC]`。

### 11.2 关键日志解读

**`[LAYOUT]` 一行示例**：

```
[LAYOUT] MAIN=si T=184 mainRaw=263 mainCons=175 mainVowel=88
         tailCons=0 endingReserve=0
         S1=88 S2=0 S3=175
         vScale=0.693 tScale=1.000 mScale=0.703
```

- `T=184`：音符总 tick
- `mainRaw=263`：main 原始长度
- `mainCons=175`：`263 * 2/3 = 175`，2/3 上限生效
- `mainVowel=88`：`263 - 175 = 88`
- `S1/S2/S3`：元音 / 衔接 / main 辅音三段需求
- `vScale/tScale/mScale`：三段各自的缩放系数

验算：`actualVowel = min(88, 92) = 88`（元音需求 < T/2），`remaining = 96`，`tScale = 1`（S2=0），`mScale = 96/175 = 0.549`。`consCap = 88*1.5 = 132 > 96`，不触发软削。

**`[VC]` 一行示例**：

```
[VC] alias=A' s vcLength=43 mainOverlap=68 vcPos=25
     notePos=53640 noteDur=87
[VC] prevPos=53520 prevDur=120 prevEnd=53640
     | vcAbs=53665 overrun=-25 gap=0
```

- `vcLength=43`：VC 视觉宽度
- `vcPos=25`：VC 位置，正数说明 VC 侵入音符内部（异常）
- `overrun=-25`：负数说明 VC 左边界在音符起点后 25 tick（异常）

`overrun` 应该是正数（VC 侵占前一个音符）、`vcPos` 应该是负数。**如果反了，说明 VC 被硬砍得太短。**

### 11.3 排查流程

1. 看 `[LAYOUT]` 里的 `mainCons / mainVowel` 比例
2. 看 `vScale / tScale / mScale` 是否 < 1
3. 看 `[VC]` 里的 `vcPos` 是否 < 0
4. 如果一切数值都合理但视觉不对，检查 **DLL 是否真的更新**（时间戳、是否从 `bin\` 复制）

---

*本文档及代码逻辑由 DeepSeek 辅助开发。*