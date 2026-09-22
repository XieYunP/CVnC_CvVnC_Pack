# 目录

1. [配布内容简介](#1-配布内容简介)
2. [方案介绍](#2-方案介绍)
   - [2.1 方案简介](#21-方案简介)
   - [2.2 发音符号](#22-发音符号)
     - [2.2.1 辅助符号解析](#221-辅助符号解析)
     - [2.2.2 符号展示](#222-符号展示)
   - [2.3 录音模板相关](#23-录音模板相关)
   - [2.4 oto指导](#24-oto指导)
   - [2.5 录音方案介绍](#25-录音方案介绍)
     - [2.5.1 方案配置差异](#251-方案配置差异)
     - [2.5.2 声库工作量](#252-声库工作量)
3. [下载链接](#3-下载链接)
4. [题外话及鸣谢](#4-题外话及鸣谢)

---

# 1. 配布内容简介

粤语 CVnC/CvVnC 参考 Leka 的粤语连单扩张整音和 kusamax 式的粤语 CVVC 方案而整合而成。

普通话 CVnC/CvVnC/nCV/nCvV 参考大手组各种整音方案（十月，Syo，芳梅，BHM），幽寂，鬼面，顾令，CVVCEX+ 等各种声库方案。

普通话 nCV 方案最初于 2024 年由本人完成制作偷懒式 VCV 的发音符号制定和录音量计算，而在今年九月因为中文 CVnC/CvVnC 初步完成后在被提醒下发现自己的录音表生成器配上 CVnC/CvVnC 方案的分割文件，其实已经可以做到生成类似偷懒式 VCV 的录音表。

# 2. 方案介绍

## 2.1 方案简介

vcv，cvvc，cvv 以及 vccv 指 UTAU 声库的拆音方案，均在目前全球歌声合成圈内广泛流传。

当中 C 通常指辅音，V 指元音。

而中文/粤语的 CVnC/CvVnC 方案只是通过组合不同方案的拆音方法（主要是 cvv 和 cvvc）让声库的录音量和合成质量的比达到比较均衡的状态。

当中字母的含义是：

- C = 辅音
- v = 介音
- V = 元音
- n = 尾音
- C = 辅音

音素组合：

- Cv：辅音到介母（如芳梅 BHM 等三段拆分）
- CV：辅音到元音（普通整音）
- vV：介母到元音（如芳梅 BHM 等三段拆分）
- VC：元音到辅音（如 CVVC 连续）
- Vn：元音到尾音（如 CVV 字内连续）
- nC：尾音到辅音（新增）

如此类推，那下面方案的差异是：

- CVnC：不带介母 + 尾音到辅音
- CvVnC：带介母 + 尾音到辅音
- nCV：带介母 + 尾音到整音
- nCvV：带介母 + 尾音到整音

## 2.2 发音符号

该方案符号参考 Risku 式 CVVC，大手组发布的各种整音方案（十月，Syo，芳梅，BHM），SV 的 X-SAMPA 中粤三段式方案，下列为主要区别（均使用 plus 表进行讲解，其标准以下的配置均会被不同程度的简化）。

### 2.2.1 辅助符号解析

- `'`：口型较小
- 大写：口型靠后（除了粤语 `M`、`NG` 因为避免与辅音混淆所以大写）
- `_`：字内衔接（如 `xi_`、`iA_`、`_Au`）
- `~`：尾音（如 `u~ a`）

### 2.2.2 符号展示

**中文普通话：**

辅音：

<img width="281" height="475" alt="普通话辅音" src="https://github.com/user-attachments/assets/76e80bda-1018-4acf-83de-daabcb31b145" />

纯元音：

<img width="88" height="229" alt="普通话纯元音" src="https://github.com/user-attachments/assets/b459a359-64ab-4574-8cba-2a3f1411b3cd" />

衔接部：

<img width="345" height="285" alt="普通话衔接" src="https://github.com/user-attachments/assets/c4e1bf5a-7ca0-447b-bfa7-cd0787d94352" />

尾音：

<img width="145" height="285" alt="普通话尾音" src="https://github.com/user-attachments/assets/07f35589-b577-4c2c-aedf-d93495613915" />

**中文粤语：**

辅音：

<img width="286" height="304" alt="粤语辅音" src="https://github.com/user-attachments/assets/0e62ae4e-5a1b-45fb-92eb-efe0bac87c2c" />

纯元音：

<img width="126" height="170" alt="粤语纯元音" src="https://github.com/user-attachments/assets/f36fcb09-0893-433b-b7c7-e7bba9f3b12a" />

衔接部：

<img width="348" height="608" alt="粤语衔接" src="https://github.com/user-attachments/assets/b028028f-c6b8-49fd-bb70-6318c6788613" />

尾音：

<img width="136" height="342" alt="粤语尾音" src="https://github.com/user-attachments/assets/035c65dd-c7e3-4a44-8d85-d7fc8f5cce49" />

## 2.3 录音模板相关

CVnC/CvVnC 方案默认 120bpm 五字表，而普通话 nCV 是 120bpm 八字表，可以在圈内找寻任何符合需求的录音表进行录制，如有需要可以使用附带的录音表生成器进行录制。

## 2.4 oto指导

### 小白 中文 CvVnC：

#### Cv：辅音到介母

<img width="624" height="521" alt="辅音到介母(zu_)" src="https://github.com/user-attachments/assets/6ceb5d5a-59c2-4df2-b06d-58eecbac67bc" />

#### CV：辅音到元音（整音）

<img width="647" height="541" alt="辅音到元音(yO)" src="https://github.com/user-attachments/assets/2707373b-e3b9-42a0-84c7-a2ef7e9a1697" />

#### vV：介母到元音

<img width="633" height="516" alt="介母到元音(iA_)" src="https://github.com/user-attachments/assets/ec1a8513-f165-4297-8b02-5270a0ea3144" />

#### VC：元音到辅音

<img width="620" height="526" alt="元音到辅音(a b)" src="https://github.com/user-attachments/assets/90794365-5628-4306-92a6-8f5d8e4ead9d" />

#### Vn：元音到尾音

<img width="621" height="536" alt="元音到尾音(_a&#39;i)" src="https://github.com/user-attachments/assets/e5a699b4-4c6a-4ae6-83df-43da7860c832" />

#### nV：尾音到元音

<img width="577" height="527" alt="尾音到元音(u~ O)" src="https://github.com/user-attachments/assets/cbc54f1c-e8c4-499c-8e75-43c36076ccb5" />

#### nC：尾音到辅音

<img width="586" height="526" alt="尾音到辅音(i~ q)" src="https://github.com/user-attachments/assets/e5edda9d-ed91-4642-9c4c-a640fd9d3a82" />

#### - C：开头辅音

<img width="579" height="525" alt="开头辅音（- r）" src="https://github.com/user-attachments/assets/91ebdc8e-48ca-4e7e-bf06-fbacc9171c4a" />

<img width="578" height="537" alt="开头辅音（- t）" src="https://github.com/user-attachments/assets/56db036f-f328-4ef2-9e34-b30bbc349523" />

<img width="618" height="521" alt="开头辅音（- b）" src="https://github.com/user-attachments/assets/12d4fab0-5f42-4ed3-88fc-bc35fdfe8fc4" />

#### - V：开头元音

<img width="581" height="522" alt="开头元音(- 7)" src="https://github.com/user-attachments/assets/38b9011e-bd72-4bc1-8913-51ae4eb09cc1" />

#### n R：尾音到结束

<img width="586" height="522" alt="尾音结尾(n~ R)" src="https://github.com/user-attachments/assets/2163ec56-59e8-438e-9c53-d044831ccbfd" />

#### V R：元音到结束

<img width="582" height="520" alt="元音结尾(o R)" src="https://github.com/user-attachments/assets/a9c6fff5-3103-468a-8c9b-f584c3758362" />
<br>

### ShiiseName_u8BY 粤语 CVnC

#### CV：辅音到元音（整音）

<img width="578" height="548" alt="整音(d9)" src="https://github.com/user-attachments/assets/0ec7b22c-0d18-4823-9469-bdba10b297a9" />

#### VC：元音到辅音

<img width="581" height="524" alt="元音到辅音(NG f)" src="https://github.com/user-attachments/assets/cf719427-ab49-4e2a-98e9-400679145c1d" />

#### Vn：元音到尾音

<img width="586" height="530" alt="元音到尾音(_a&#39;t)" src="https://github.com/user-attachments/assets/10568c04-de0f-4206-9d0d-b4dd1ca49583" />

<img width="597" height="546" alt="元音到尾音(_A&#39;k)" src="https://github.com/user-attachments/assets/c024eb99-a853-4de6-a29f-ef51f29cb9ba" />

<img width="583" height="533" alt="元音到尾音(_a&#39;m)" src="https://github.com/user-attachments/assets/7ef250dd-c3a8-46fc-bae0-d8651a38bb0a" />

#### nV：尾音到元音

<img width="469" height="538" alt="尾音到元音(yu~ A)" src="https://github.com/user-attachments/assets/30610cbf-8930-4d1d-90e6-a583d1f65344" />

<img width="540" height="539" alt="尾音到元音(p~ e&#39;)" src="https://github.com/user-attachments/assets/c0e0ae4f-4059-42f3-ae7f-af65a30d67db" />

#### nC：尾音到辅音

<img width="580" height="531" alt="尾音到辅音(t~ c)" src="https://github.com/user-attachments/assets/e7b47aaa-5f5e-4e4c-acad-9c294725ac33" />

<img width="469" height="529" alt="尾音到辅音(p~ g)" src="https://github.com/user-attachments/assets/51d0f250-615c-4ac3-b71a-95b159e49245" />

<img width="570" height="529" alt="尾音到辅音(m~ sy)" src="https://github.com/user-attachments/assets/0e454059-04f1-4e4a-bb53-d9acf14cd9e3" />

#### - CV：开头整音

<img width="580" height="519" alt="开头整音(- te&#39;)" src="https://github.com/user-attachments/assets/cdc86fba-3148-4011-94a1-e8a776c88a76" />

#### - V：开头元音

<img width="573" height="532" alt="开头元音(- M)" src="https://github.com/user-attachments/assets/f3a5c6aa-c0ef-4ded-8d48-5dbe6546c031" />

#### n R：尾音到结束

<img width="582" height="546" alt="尾音到结束(t~ R)" src="https://github.com/user-attachments/assets/834f1124-2207-401a-a0ec-337d6435df65" />

#### V R：元音到结束

<img width="617" height="536" alt="元音结尾(M R)" src="https://github.com/user-attachments/assets/8879ea8f-bb91-4d9d-b0b7-9c4f0a1a7b28" />

## 2.5 录音方案介绍

- **plus**：豪华表，面向专业调声人士，录音量最多
- **std**：标准表，最低发音区分基准，适合标准配布和调声
- **lite**：简约表，在 std 和 slim 的平衡点，适合想简单体验此方案而且想因此配布的人士
- **slim**：极简表，对象是想录个简单的声库进行测试和体验的人，许多发音被合并，所以对于调音来说发音可能会比较模糊

### 2.5.1 方案配置差异

**简写词汇表：**

- `seq`：顺序（开头有 `a_b_c`……）
- `rep`：重复式（开头有 `a_a_a`）
  - （本次配布没有此配置）
- `int`：隔断式（开头有 `a_R_b_R_c`……）
  - （本次配布没有此配置）
- `none`：无上述强制生成部分

- `-cv`：开头整音
- `-c`：开头辅音

- `MNG_ex`：添加 `hm`、`hng`、`m`、`ng` 扩展发音（只在粤语方案里）

**普通话 CVnC：** 在 CvVnC 基础上合并介母

- **plus**：拆 `gw/kw/zhw/shw/chw/rw/cw/sw/zw/c0/ch0/h0/r0/s0/v0`，`h` 区分 `h/h0/hh/hw/hx`
- **std**：拆 `gw/kw/zhw/shw/chw/rw/cw/sw/zw`，`c0/ch0/h0/r0/s0` 合并回其辅音类里，`v` 归类到 `y`，`h` 合并到 `h/hw`

**普通话 CvVnC：**

- **plus**：拆 `gw/kw/zhw/shw/chw/rw/cw/sw/zw/c0/ch0/r0/s0/v0`，`h` 区分 `h/h0/hh/hw/hx`，`yue/yuan` 的元音区分成 `E'`，`yong`、`you` 的半元音归类为 `v`
- **std**：拆 `gw/kw/zhw/shw/chw/rw/cw/sw/zw`，但是 `h` 合并到 `h/hw`，`yue/yuan` 的元音合并到 `e'`，`yong`、`you` 的半元音随着辅音合并也归类为 `y`
- **lite**：`j/x/l/m/n` 合并，半元音参与介母拆分
- **slim**：`ch/h/q/s/sh/x` 合并，所有元音基本合并

**粤语 CVnC：** 在 CvVnC 基础上合并介母

**粤语 CvVnC：**

- **plus**：辅音区分为 `c/cy`、`g/gw`、`h/h0/hh/hm/hng/hx/hy`、`j/jy`、`k/kw`、`l/ly`、`n/ny`、`s/sy`、`z/zy`（包含 `MNG_ex`）
- **std**：同上，但是半元音也参与拆分了，例如 `ji_`、`jyu_`，和 `wo_`、`wA'_`，衔接部也多拆了 `yu8_` 和 `yu9_`（包含 `MNG_ex`）
- **lite**：辅音合并为 `c`、`g`、`h`、`j`、`k`、`l`、`n`、`s`、`z`，还在上述基础上多拆了 `wo_`
- **slim**：同上，而且 `k~`、`p~`、`t~` 尾额外合并为 `kpt~`

### 2.5.2 声库工作量

**canton_CvVnC_CVNC 五字表**

| 方案 | 录音量 | oto量 |
|---|---:|---:|
| `ZH_cantonese_CVnC_MNG_ex_seq_-cv` | 392 | 1219 |
| `ZH_cantonese_CvVnC_plus_MNG_ex_seq_-c` | 368 | 1040 |
| `ZH_cantonese_CvVnC_std_MNG_ex_seq_-c` | 309 | 888 |
| `ZH_cantonese_CvVnC_lite_seq_-c` | 241 | 684 |
| `ZH_cantonese_CvVnC_slim_seq_-c` | 233 | 634 |

**ZH_CvVnC 五字表**

| 方案 | 录音量 | oto量 |
|---|---:|---:|
| `ZH_CvVnC_plus_-cv_seq` | 426 | 1383 |
| `ZH_CvVnC_std_-cv_seq` | 354 | 1213 |
| `ZH_CvVnC_lite_-c_seq` | 268 | 854 |
| `ZH_CvVnC_slim_-c_seq` | 233 | 755 |

**ZH_nCvV_nCV 八字表**

| 方案 | 录音量 | oto量 |
|---|---:|---:|
| `ZH_nCV_std_-cv_none` | 1205 | 5062 |
| `ZH_nCvV_slim_-cv_none` | 799 | 3354 |

# 3. 下载链接

- 录音表生成器及附带的分割文件：<https://github.com/XieYunP/reclisrgen_plus_plus>
- 自动标注器扩展：<https://github.com/XieYunP/TextGrid2oto_cvnc_plugin>
- CVvC/CvVvC 制作包：<https://github.com/XieYunP/CVnC_CvVnC_Pack>

# 4. 题外话及鸣谢

我本来想打算严格遵从英语简称把方案命名成 `CmVcC`：

- `C` = 辅音 = Consonant
- `m` = 介音 = Medial
- `V` = 元音 = Vowel
- `c` = 尾音 = Coda
- `C` = 辅音 = Consonant

但是作为参考的 `cvv`（连单音）声库方案的 `vv` 表达字内连续，而两个 `c` 组合在一起很容易以为是辅音到辅音，在“小白君”的建议下考虑中文不需要辅音到辅音的链接，所以把这个方案叫做中文 `vccv`（本人对此方案的旧称）显然是不算更准确的那一档，所以退而求其次把 `c` 改成了 `n`（nasal）。

特此感谢“小白君”对方案和自动标记器扩展制作的指导，以及感谢它录制了 `cvvnc` 声库。

截止到目前文档发布的 2026 年 x 月 x 日，还没有类似的 `nCV` 方案的声库完成 oto 标记，所以本人在第一个 `nCV` 方案的声库配布之前都会在沟通同意下免费制作 `nCV` 方案的声库。

特此感谢“AC和洛必达先生”对 `CVnC/CvVnC` 音素器的指导，中之人找寻，和 `nCV` 方案制作的提醒。
