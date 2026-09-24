# 目录

1. [配布内容简介](#1-配布内容简介)
2. [方案介绍](#2-方案介绍)
   - [2.1 方案简介](#21-方案简介)
   - [2.2 发音符号](#22-发音符号)
     - [2.2.1 辅助符号和术语解析](#221-辅助符号和术语解析)
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

### 2.2.1 辅助符号和术语解析

**辅助符号：**

- `'`：口型较小
- 大写：口型靠后（除了粤语 `M`、`NG` 因为避免与辅音混淆所以大写）
- `_`：字内衔接（如 `xi_`、`iA_`、`_Au`）
- `~`：尾音（如 `u~ a`）

> **重要声明：** 这些是对完全不懂的人做的查找表，相对懂行的可以跳过这部分，以免被绕倒。

**音频相关：**

- **绝对静音**：指真的没有明显声音，在普通录制值只有基本噪声的情况，如果是手搓，那理论上可以做到完全静音。
- **静音**：声源没有发出声音，当然以目前的技术很难做到绝对静音，除非是无生物那种手搓的，所以这里指的是声源没有主动发出声音。
- **非静音/发声/发出声音**：声源主动发出声音。

**发声相关：**

- **音素**：通常指 `oto.ini` 里的别名，如 `a_e_i_o_u` 里单独的 `a`、`e`、`i`、`o`、`u`。
- **音素类型**：音素类型值是 cvvc、cv、cvv、vccv、vcv 那种不同方案里描述 cc、vv、cv、vc 连接类型的形式，是方案里最基础的音素类型拆分，详细可以从下方音素相关的音素类型里详阅。

**音素相关：**

**术语所对应的语音学概念：**

- **辅音**：在元音之前的发音，通常是作辅助用途，如发出清音 s、f 和浊音 l、n，如同中文拼音 bpmf，时间先于元音开始，通常早于一拍开始，如普通话的声母。
- **元音**：在辅音之后的发音，元音的开始通常落在拍子上，例如 a、e、i、o、u，如普通话的韵母。
- **半元音**：不是元音，是拿元音当作辅音去使用，例如 yi、wu、yu。
- **双元音**：使用两种不同的元音或者尾音进行拼接，以区分出更多元音，例如 ao、ong、ei，如普通话的韵尾。
- **介母**：本质上是元音，但是只介于元音之间，用于区分出更多元音，例如 lia 和 la 之间的区别，但是介母没有纯元音，因为发声调度，开头的介母会有半元音作辅音，当然有些发音可能会把半元音弄得几乎消失，但是目前普通话没有单独发出介母的需要，所以默认介母带辅音。
- **尾音**：这里有两个含义，在语言学里通常是指 ai、ao、iu 那种带着韵尾的尾音，但是在声库制作的分割表里，凡是尾部无论是否韵尾还是韵母，尾部连接辅音或者整音都叫作尾音，本文内进行了简单区分，但因为之前的视频对此的区分很模糊，所以特此说明。
- **纯元音**：没有辅音，只有元音类的发音，所以 ya 不算纯元音。

**音素类型：**

- **普通整音**：CV，如 ba，组合了辅音和元音类的发音，常见特点是左右方都有发音，在所有声库制作里非常常见，在声库制作里很常见。
- **开头辅音**：- C，例子如 - k，记录在静音到该辅音的形式，这形式在英文 vccv 相当常见，而这次是自己首次把这形式搬进中文里。
- **开头整音**：- CV，例子如 - po，记录在静音到该整音的形式，普通的开头音，在声库制作里非常常见。
- **开头元音**：- V，例子如 - ao，记录在静音到该纯元音整音的形式，在声库制作里非常常见。
- **开头介母**：- Cv，如 - yv_，记录在静音到该介母整音的形式，在中文声库制作里不算很常见。
- **介母整音**：Cv，如 mi_，组合了辅音和介音类的发音，在中文 CVV 里比较常见。
- **尾音结尾**：也叫尾音到结束，记录在该纯元音到静音的形式，如 7 R，在声库制作里非常常见。
- **元音到辅音**：VC，如 u g，记录在该元音到下一个辅音的形式，在中文 CVVC 里很常见。
- **尾音到辅音**：nC，如 n~ a'，记录在该尾音到辅音的形式，在声库制作里很罕有，与尾音到元音是这方案最新的连接形式。
- **元音到尾音**：Vn，如 _Ang，记录在该元音到尾音的形式，在中文 CVV 里很常见。
- **元音到元音**：VV，如 u O，记录在该元音到元音的形式，类似中文 vcv 里的非常常见。
- **尾音到元音**：nV，如 i~ e'，记录在该尾音到元音的形式，在声库制作里很罕有，与尾音到辅音是这方案最新的连接形式。

（此处是 nCV 和 nCvV 的音素形式，本次发布没有声库示例，所以无法通过 oto 详解查看参数设置，但可以通过其他方案推测出应该的位置）

- **元音到整音**：VCV，如 a ga，记录在该元音到普通整音的形式，在中文 VCV 里很常见。
- **元音到介母整音**：VCv，如 u ji_，记录在该元音到介母整音的形式，是这方案最新的连接形式。
- **尾音到整音**：nCV，如 r'~ ge'，记录在该尾音到普通整音的形式，是这方案最新的连接形式。
- **尾音到介母整音**：nCv，如 n'~ lu_，记录在该尾音到介母整音的形式，是这方案最新的连接形式。

### 2.2.2 符号展示

该方案符号参考 Risku 式 CVVC，大手组发布的各种整音方案（十月，Syo，芳梅，BHM），SV 的 X-SAMPA 中粤三段式方案，下列为主要区别（均使用 plus 表进行讲解，其标准以下的配置均会被不同程度的简化）：

说明：每个发音的旁边都会有两个普通话或者粤语拼音来进行示例，除非该对应的符号只有一个发音；另外纯元音部分不代表所有元音，有些元音是没有纯元音形式的，可以看衔接部。

**中文普通话：**

#### 辅音

| 音素 | 拼音 1 | 拼音 2 |
|---|---|---|
| `b` | ba | bai |
| `c` | ca | cai |
| `c0` | cong | cou |
| `ch` | cha | chai |
| `ch0` | chong | chou |
| `chw` | chu | chuai |
| `cw` | cu | cuan |
| `d` | da | dai |
| `f` | fa | fan |
| `g` | ga | gai |
| `gw` | gu | gua |
| `h` | ha | hai |
| `h0` | hong | hou |
| `hh` | he | hen |
| `hw` | hu | hua |
| `hx` | hei |  |
| `jv` | jiong | jiu |
| `jy` | ji | jia |
| `k` | ka | kai |
| `kw` | ku | kua |
| `l` | la | lai |
| `ly` | li | lia |
| `m` | ma | mai |
| `my` | mi | mian |
| `n` | na | nai |
| `ny` | ni | nian |
| `p` | pa | pai |
| `qv` | qiong | qiu |
| `qy` | qi | qia |
| `r` | ran | rang |
| `r0` | rong | rou |
| `rw` | ru | ruan |
| `s` | sa | sai |
| `s0` | song | sou |
| `sh` | sha | shai |
| `sh0` | shou |  |
| `shw` | shu | shua |
| `sw` | su | suan |
| `t` | ta | tai |
| `v0` | yong | you |
| `v` | yuan | yue |
| `w` | wa | wai |
| `xv` | xiong | xiu |
| `xy` | xi | xia |
| `y` | ya | yan |
| `z` | za | zai |
| `zh` | zha | zhai |
| `zhw` | zhu | zhua |
| `zw` | zong | zou |

#### 纯元音

| 音素 | 拼音 1 | 拼音 2 |
|---|---|---|
| `7` | e |  |
| `A` | ang | ao |
| `E` | en |  |
| `O` | ong | ou |
| `V` | yu |  |
| `a` | a |  |
| `a'` | ai | an |
| `e` | eng |  |
| `e'` | ei |  |
| `er'` | er |  |
| `i` | yi |  |
| `u` | wu |  |

#### 衔接部

| 音素 | 拼音 1 | 拼音 2 |
|---|---|---|
| `VE'_` | jue | lue |
| `VE'_,_E'n` | juan | quan |
| `_Ang` | ang | bang |
| `_Au` | ao | bao |
| `_En` | ben | cen |
| `_I'ng` | bing | ding |
| `_Ong` | chong | cong |
| `_Ou` | chou | cou |
| `_Vn` | jun | qun |
| `_a'i` | ai | bai |
| `_a'n` | an | ban |
| `_e'i` | bei | dei |
| `_eng` | beng | ceng |
| `_er'` | er |  |
| `_in` | bin | jin |
| `iA_` | jiang | liang |
| `iA_,_Au` | biao | diao |
| `iO_,_Ou` | diu | liu |
| `ia_` | jia | lia |
| `ie'_` | bie | die |
| `ie'_,_e'n` | bian | dian |
| `uA_,_Ang` | chuang | guang |
| `uE_,_En` | chun | cun |
| `ua'_,_a'i` | chuai | guai |
| `ua'_,_a'n` | chuan | cuan |
| `ua_` | gua | hua |
| `ue'_,_e'i` | chui | cui |
| `uo_` | chuo | cuo |
| `vO_,_Ong` | jiong | qiong |
| `vO_,_Ou` | jiu | qiu |

#### 尾音

| 音素 | 拼音 1 | 拼音 2 |
|---|---|---|
| `7` | ce | che |
| `E'` | jue | lue |
| `I` | ci | si |
| `V` | ju | lv |
| `a` | a | ba |
| `e'` | bie | die |
| `i` | bi | di |
| `ir'` | chi | ri |
| `i~` | ai | bai |
| `ng~` | ang | bang |
| `n~` | an | ban |
| `o` | bo | chuo |
| `r'~` | er |  |
| `u` | bu | chu |
| `u~` | ao | bao |

**中文粤语：**

#### 辅音

| 音素 | 拼音 1 | 拼音 2 |
|---|---|---|
| `b` | baa | baai |
| `c` | caa | caai |
| `cy` | ceoi | ceon |
| `d` | daa | daai |
| `f` | faa | faai |
| `g` | gaa | gaai |
| `gw` | geoi | goe |
| `h` | haa | haai |
| `h0` | heoi | ho |
| `hh` | him | hin |
| `hm` | hM |  |
| `hng` | hNG |  |
| `hx` | hei | hek |
| `hy` | hoe | hoeng |
| `j` | jaa | jaai |
| `jy` | jeoi | jeon |
| `k` | kaa | kaai |
| `kw` | keoi | koe |
| `l` | laa | laai |
| `ly` | leoi | leon |
| `m` | maa | maai |
| `n` | naa | naai |
| `ng` | ngaa | ngaai |
| `ny` | neoi | neot |
| `p` | paa | paai |
| `s` | saa | saai |
| `sy` | seoi | seon |
| `t` | taa | taai |
| `w` | waa | waai |
| `z` | zaa | zaai |
| `zy` | zeoi | zeon |

#### 纯元音

| 音素 | 拼音 1 | 拼音 2 |
|---|---|---|
| `A` | aang | aau |
| `A'` | aa | aai |
| `M` | M |  |
| `NG` | NG |  |
| `O` | ok | ong |
| `a'` | ai | ak |
| `e'` | e | ei |
| `o` | o | oi |
| `o'` | ou |  |

#### 衔接部

| 音素 | 拼音 1 | 拼音 2 |
|---|---|---|
| `_8n` | ceon | deon |
| `_8t` | ceot | deot |
| `_8yu` | ceoi | deoi |
| `_9k` | coek | doek |
| `_9ng` | coeng | goeng |
| `_A'i` | aai | baai |
| `_A'k` | aak | baak |
| `_A'm` | aam | caam |
| `_A'n` | aan | baan |
| `_A'p` | aap | caap |
| `_A't` | aat | baat |
| `_Ang` | aang | baang |
| `_Au` | aau | baau |
| `_Ik` | bik | cik |
| `_Ing` | bing | cing |
| `_Ok` | bok | cok |
| `_Ong` | bong | cong |
| `_Ot` | got | hot |
| `_Uk` | buk | cuk |
| `_Ung` | bung | cung |
| `_a'i` | ai | bai |
| `_a'k` | ak | bak |
| `_a'm` | am | bam |
| `_a'n` | ban | can |
| `_a'ng` | ang | bang |
| `_a'p` | ap | cap |
| `_a't` | bat | cat |
| `_a'u` | au | bau |
| `_e'i` | bei | dei |
| `_e'k` | bek | cek |
| `_e'm` | lem |  |
| `_e'ng` | beng | ceng |
| `_e'p` | gep |  |
| `_e'u` | deu |  |
| `_im` | cim | dim |
| `_in` | bin | cin |
| `_ip` | cip | dip |
| `_it` | bit | cit |
| `_iu` | biu | ciu |
| `_o'u` | bou | cou |
| `_on` | gon | hon |
| `_oyu` | coi | doi |
| `_uk` | uk |  |
| `_un` | bun | fun |
| `_ung` | ung |  |
| `_ut` | but | fut |
| `_uyu` | bui | fui |
| `_yun` | cyun | dyun |
| `_yut` | cyut | dyut |
| `wA'_` | gwaa | kwaa |
| `wA'_,_A'i` | gwaai | kwaai |
| `wA'_,_A'k` | gwaak | waak |
| `wA'_,_A'n` | gwaan | waan |
| `wA'_,_A't` | gwaat | waat |
| `wA_,_Ang` | gwaang | kwaang |
| `wI_,_Ik` | gwik | kwik |
| `wI_,_Ing` | gwing |  |
| `wO_,_Ok` | gwok | kwok |
| `wO_,_Ong` | gwong | kwong |
| `wa'_,_a'i` | gwai | kwai |
| `wa'_,_a'n` | gwan | kwan |
| `wa'_,_a'ng` | gwang | wang |
| `wa'_,_a't` | gwat | wat |

#### 尾音

| 音素 | 拼音 1 | 拼音 2 |
|---|---|---|
| `9` | doe | goe |
| `A'` | aa | baa |
| `M` | M | hM |
| `NG` | NG | hNG |
| `e'` | be | ce |
| `i` | ci | ji |
| `i~` | aai | ai |
| `k~` | aak | ak |
| `m~` | aam | am |
| `ng~` | aang | ang |
| `n~` | aan | baan |
| `o` | bo | co |
| `p~` | aap | ap |
| `t~` | aat | baat |
| `u` | fu | gu |
| `u~` | aau | au |
| `yu` | cyu | jyu |
| `yu~` | bui | ceoi |

## 2.3 录音模板相关

CVnC/CvVnC 方案默认 120bpm 五字表，而普通话 nCV 是 120bpm 八字表，可以在圈内找寻任何符合需求的录音表进行录制，如有需要可以使用附带的录音表生成器进行录制。

## 2.4 oto指导

### 小白 中文 CvVnC：

#### Cv：辅音到介母

<img width="624" height="521" alt="辅音到介母(zu_)" src="https://github.com/user-attachments/assets/6ceb5d5a-59c2-4df2-b06d-58eecbac67bc" />

说明：标注的时候注意右边界只有介母，而 z、c 那种塞擦音为了与尾音到辅音的衔接链接好，通常带点衔接比较好。如果发得非常干脆，那可以像 b、p 爆破音只标记空白，但是要看你整体是怎么样了，否则容易出双重辅音或者辅音淡化。该类型辅音的左边界在辅音开始或者带点空白，预留给那种发得很清脆那种塞擦音；如果过浊导致连起来，那甚至会有负数重叠来增加辅音前空白。

#### CV：辅音到元音（整音）

<img width="578" height="534" alt="辅音到元音(yO)" src="https://github.com/user-attachments/assets/938d9540-54c7-425b-8f63-b42ec5800ad0" />

说明：半元音标记其实是最难的，因为你不能靠听声音，因为介母的听感还是半元音辅音的听感。标记的时候需要看频谱或者波形，不要把介母的部分划入辅音里。

#### vV：介母到元音

<img width="633" height="516" alt="介母到元音(iA_)" src="https://github.com/user-attachments/assets/ec1a8513-f165-4297-8b02-5270a0ea3144" />

说明：这个算是容易，有时介母会拉得比较长，下面的谐波变形之后还有一些部分没有跟上，重叠部分可以偶尔放在完全变成元音的部分。

#### Vn：元音到尾音

<img width="621" height="536" alt="元音到尾音(_a&#39;i)" src="https://github.com/user-attachments/assets/e5a699b4-4c6a-4ae6-83df-43da7860c832" />

说明：这点如同中文扩张整音或者字内连续音（CV，CVV）那种标记方法，只是如同一些 syo 表一样放在字内的。建议看频谱和波形，我推荐频谱：左边界在元音，重叠在元音开始变化的时候，预发声（先行发声/红线）在尾音开始，固定在尾音稳定的部分，右边界在尾音开始结束的部分。

#### VC：元音到辅音

<img width="620" height="526" alt="元音到辅音(a b)" src="https://github.com/user-attachments/assets/90794365-5628-4306-92a6-8f5d8e4ead9d" />

说明：这点如同 cvvc 的标记：重叠在口型开始变化的时候，预发声在元音结束的部分，而不是噪声结束的部分，因为可能是房间混响或者嘴巴噪音。连接爆破音的口型变化会变得很短，所以重叠甚至可以再靠后一点增加重叠部分。当然重叠部分的长度要看素材了：有些元音稳定但交叉处相位问题可以减少重叠，有些元音多变反而没有相位问题可以增加重叠让淡入更自然。

#### nC：尾音到辅音

<img width="586" height="526" alt="尾音到辅音(i~ q)" src="https://github.com/user-attachments/assets/e5edda9d-ed91-4642-9c4c-a640fd9d3a82" />

说明：只要记得左边界在尾音不是元音就行，其余的如同 cvvc 和上述讲到的一样，右边界大约预计在你预计会把下一个发音的重叠放在哪个位置。在衔接例子里，左边界只包括 ei 的 i 尾而不是整个 ei。

#### VV：元音到元音

<img width="626" height="536" alt="元音到元音(a A)" src="https://github.com/user-attachments/assets/112b2c5d-ac89-4bab-910a-debacf938d1a" />

说明：通常 vcv 和 cvvc 里的形式，标记的时候看频谱和波形比较难，注意要听声音捉住音高或口型变化的部分。

#### nV：尾音到元音

<img width="577" height="527" alt="尾音到元音(u~ O)" src="https://github.com/user-attachments/assets/cbc54f1c-e8c4-499c-8e75-43c36076ccb5" />

说明：类似 vcv 的形式，但是连接辅音，记得连接辅音，如 VV。

#### - C：开头辅音

<img width="579" height="525" alt="开头辅音（- r）" src="https://github.com/user-attachments/assets/91ebdc8e-48ca-4e7e-bf06-fbacc9171c4a" />

说明：这点注意下，有些声库的 r、y、w 擦音化，这时候不要听声音，看频谱，忽略波形或者频谱里的清音，要记得最重要是那些谐波。

<img width="578" height="537" alt="开头辅音（- t）" src="https://github.com/user-attachments/assets/56db036f-f328-4ef2-9e34-b30bbc349523" />

说明：对于 t、k 那种送气的比较容易，包括在开头就行，但是比较简单的方案辅音可能被过度合并了，所以注意不能完全截取，只能截个开头。

<img width="618" height="521" alt="开头辅音（- b）" src="https://github.com/user-attachments/assets/12d4fab0-5f42-4ed3-88fc-bc35fdfe8fc4" />

说明：对于 b 那种爆破音，其实有点困难，既要包括在内而且不再次双重辅音或者淡化过度，的确要下很多功夫。如果以下 b 比较浊和送气那比较容易，可以把鼻音和送气收录；但偏向爆破的则有点困难，因为几乎没有，那只能记录发声前的静音了，或者像这展示一样尽量标记。如果是整音的话，如 ba，那可能左边界靠左约五到十毫秒，把重叠部分放在发音开始几个毫秒。

#### - V：开头元音

<img width="581" height="522" alt="开头元音(- 7)" src="https://github.com/user-attachments/assets/38b9011e-bd72-4bc1-8913-51ae4eb09cc1" />

说明：相对容易的标记，确保预发声在元音开始就行，其他保持相对的位置。

#### n R：尾音到结束

<img width="585" height="522" alt="尾音结尾(n~ R)" src="https://github.com/user-attachments/assets/4c4b5432-4f69-41a7-9413-dcb29fa17c62" />

说明：相对容易的标记，截到的是只有 n 尾就行，其他保持相对的位置，左边界可在气声或者混响完全结束的地方（假如录音素材较差的话）。

#### V R：元音到结束

<img width="582" height="520" alt="元音结尾(o R)" src="https://github.com/user-attachments/assets/a9c6fff5-3103-468a-8c9b-f584c3758362" />

说明：相对容易的标记，确保预发声在元音结束就行，其他保持相对的位置。

### ShiiseName_u8BY 粤语 CVnC：

#### CV：辅音到元音（整音）

<img width="578" height="548" alt="整音(d9)" src="https://github.com/user-attachments/assets/0ec7b22c-0d18-4823-9469-bdba10b297a9" />

说明：爆破音的标记相对简单，与其他标记（普通话，粤语 syo 等）差不多，只要记得 j 对应的是普通话半元音的 y 就行。

#### Vn：元音到尾音

<img width="586" height="530" alt="元音到尾音(_a&#39;t)" src="https://github.com/user-attachments/assets/10568c04-de0f-4206-9d0d-b4dd1ca49583" />

说明：粤语入声记得把预发声放在结束的部分，固定看情况放在声音完全到相对静音的部分。

<img width="597" height="546" alt="元音到尾音(_A&#39;k)" src="https://github.com/user-attachments/assets/c024eb99-a853-4de6-a29f-ef51f29cb9ba" />

以这个例子，混响有点大的话那就看标注者的经验了。

<img width="583" height="533" alt="元音到尾音(_a&#39;m)" src="https://github.com/user-attachments/assets/7ef250dd-c3a8-46fc-bae0-d8651a38bb0a" />

其实这些入声与非入声与粤语 syo 差不多，不要把右边界放到尾音结束，而是放在开始结束的位置就行。

#### nV：尾音到元音

<img width="469" height="538" alt="尾音到元音(yu~ A)" src="https://github.com/user-attachments/assets/30610cbf-8930-4d1d-90e6-a583d1f65344" />

说明：记得不要把左边界放得太左，重叠在尾音开始结束就行，其余与普通整音一样。

<img width="540" height="539" alt="尾音到元音(p~ e&#39;)" src="https://github.com/user-attachments/assets/c0e0ae4f-4059-42f3-ae7f-af65a30d67db" />

对于入声后的元音，气泡音判定为辅助且提前一拍的发音，预发声放在前方；如果比较短或者气泡音不明显到成为辅音的程度，那就放在于发声后一点的地方。

#### VV：元音到元音

<img width="575" height="528" alt="元音到元音(9 NG)" src="https://github.com/user-attachments/assets/c7e6d1f1-efac-4d99-8d46-466508468be7" />

说明：记得预发声放在元音开始就行，的确有点困难，常见失误是把预发声放在靠右的地方，导致听感抢拍。

#### VC：元音到辅音

<img width="581" height="524" alt="元音到辅音(NG f)" src="https://github.com/user-attachments/assets/cf719427-ab49-4e2a-98e9-400679145c1d" />

说明：在粤语里，NG 和 M 归类为元音，为什么大写，因为还有辅音 m 和 ng，另外是如果发音比较模糊的话，固定可以放在谐波完全结束后，而预发声可以在辅音开始的地方。

#### nC：尾音到辅音

<img width="580" height="531" alt="尾音到辅音(t~ c)" src="https://github.com/user-attachments/assets/e7b47aaa-5f5e-4e4c-acad-9c294725ac33" />

说明：对于入声到辅音的地方，标记于开头辅音很像，只是左边界和重叠部分是在记录入声到辅音的空白。

<img width="469" height="529" alt="尾音到辅音(p~ g)" src="https://github.com/user-attachments/assets/51d0f250-615c-4ac3-b71a-95b159e49245" />

说明：如果是爆破音 g 那种，最好带点尾就行，不用覆盖整个辅音，因为可能有些爆破音因为发音过浊导致重叠需要设置成负数（左于左边界）来增加空白部分的情况。

<img width="570" height="529" alt="尾音到辅音(m~ sy)" src="https://github.com/user-attachments/assets/0e454059-04f1-4e4a-bb53-d9acf14cd9e3" />

说明：而最后一种情况最容易，很明显看到尾音 m~ 开始，结束，辅音 sy 开始，到预想中下一个发音的重叠区域比较靠右的情况。如果尾音太短，可以轻微带下元音一点点，因为最开头是交叉淡入的最初淡入的部分，相对不明显。

#### - CV：开头整音

<img width="580" height="519" alt="开头整音(- te&#39;)" src="https://github.com/user-attachments/assets/cdc86fba-3148-4011-94a1-e8a776c88a76" />

说明：记得重叠截取到静音和辅音开始之间的点，因为这是开头音不是普通整音。

#### - V：开头元音

<img width="573" height="532" alt="开头元音(- M)" src="https://github.com/user-attachments/assets/f3a5c6aa-c0ef-4ded-8d48-5dbe6546c031" />

说明：偶尔如果辅音容易出杂音或者过长，可以适度靠右甚至超过预发声，不过这个例子感觉不是最优解，因为没有发现需要淡化的地方，重叠也许可以靠前一点。

#### n R：尾音到结束

<img width="582" height="546" alt="尾音到结束(t~ R)" src="https://github.com/user-attachments/assets/834f1124-2207-401a-a0ec-337d6435df65" />

说明：尾音到结束对于入声有点尴尬，因为入声之后是空白，所以目前处理方法都是截取空白，目前并不确认这是不是最优解。

#### V R：元音到结束

<img width="617" height="536" alt="元音结尾(M R)" src="https://github.com/user-attachments/assets/8879ea8f-bb91-4d9d-b0b7-9c4f0a1a7b28" />

说明：很简单，只要注意预发声是否忽略混响放在元音结束之后。

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
- **slim**：`ch/h/q/s/sh/x` 合并，至此所有元音辅音都基本合并

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
