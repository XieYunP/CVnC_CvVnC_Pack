using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Serilog;
namespace OpenUtau.Core
{
    [Phonemizer("ZH CVnC/CvVnC Phonemizer", "ZH CVnC/CvVnC", language: "ZH")]
    public class ZHCVnCCvVnCPhonemizer : Phonemizer
    {
        private USinger? singer;
        private readonly Dictionary<string, CVnCRule> rules = new();

        public override void SetSinger(USinger singer)
        {
            if (this.singer == singer)
            {
                return;
            }
            this.singer = singer;
            rules.Clear();
            if (this.singer == null)
            {
                return;
            }
            LoadSchemeFile();
        }

        private void LoadSchemeFile()
        {
            try
            {
                string schemeFile = Path.Combine(singer!.Location, "scheme.ini");
                if (!File.Exists(schemeFile))
                {
                    string pluginDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
                    schemeFile = Path.Combine(pluginDir, "scheme.ini");
                }

                if (!File.Exists(schemeFile))
                {
                    Log.Warning("scheme.ini not found");
                    return;
                }

                using var reader = new StreamReader(schemeFile);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    {
                        continue;
                    }

                    int eq = trimmed.IndexOf('=');
                    if (eq <= 0)
                    {
                        continue;
                    }

                    string pinyin = trimmed.Substring(0, eq).Trim();
                    string ruleStr = trimmed.Substring(eq + 1).Trim();

                    var parts = ParseRuleString(ruleStr);
                    if (parts.Count < 4)
                    {
                        continue;
                    }

                    string prefix = parts[0].Trim();
                    string main = parts[1].Trim();

                    var transitions = new List<string>();
                    string transitionStr = parts[2].Trim().Trim('"');
                    if (!string.IsNullOrEmpty(transitionStr))
                    {
                        foreach (var t in transitionStr.Split(','))
                        {
                            string tt = t.Trim();
                            if (!string.IsNullOrEmpty(tt))
                            {
                                transitions.Add(tt);
                            }
                        }
                    }

                    string ending = parts[3].Trim().Trim('"');

                    rules[pinyin] = new CVnCRule
                    {
                        Prefix = prefix,
                        Main = main,
                        TransitionPhonemes = transitions,
                        Ending = ending,
                    };
                }

                Log.Information($"Loaded {rules.Count} CVnC/CvVnC rules");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load scheme file");
            }
        }

        private static List<string> ParseRuleString(string ruleStr)
        {
            var parts = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            foreach (char c in ruleStr)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    current.Append(c);
                }
                else if (c == ',' && !inQuotes)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                parts.Add(current.ToString());
            }

            return parts;
        }

        protected virtual string[] Romanize(IEnumerable<string> lyrics)
        {
            return BaseChinesePhonemizer.Romanize(lyrics);
        }

        public static Note[] ChangeLyric(Note[] group, string lyric)
        {
            var oldNote = group[0];
            group[0] = new Note
            {
                lyric = lyric,
                phoneticHint = oldNote.phoneticHint,
                tone = oldNote.tone,
                position = oldNote.position,
                duration = oldNote.duration,
                phonemeAttributes = oldNote.phonemeAttributes,
            };
            return group;
        }

        public override void SetUp(Note[][] groups, UProject project, UTrack track)
        {
            base.SetUp(groups, project, track);
            var romanizedLyrics = Romanize(groups.Select(g => g[0].lyric));
            Enumerable.Zip(groups, romanizedLyrics, ChangeLyric).Last();
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours)
        {
            var note = notes[0];
            string lyric = note.lyric ?? "";

            // 音色属性（索引 0 用于主音素，索引 1 用于辅音微调）
            var attr0 = GetAttr(note, 0);
            var attr1 = GetAttr(note, 1);

            // 整个音符组（含所有 "+"）的总时长
            int totalDuration = notes.Sum(n => n.duration);
            int endTick = note.position + totalDuration;

            // 找规则
            if (!rules.TryGetValue(lyric, out var currentRule))
            {
                return MakeSimpleResult(lyric, attr0);
            }

            // 前后邻居信息（"+" 不应出现在 prevNeighbour/nextNeighbour 中，
            // 因为 OpenUtau 已经把它们合并进 notes[] 了；这里做防御性判断）
            bool hasPrev = prevNeighbour.HasValue
                && !string.IsNullOrEmpty(prevNeighbour.Value.lyric)
                && prevNeighbour.Value.lyric != "+";
            bool hasNext = nextNeighbour.HasValue
                && !string.IsNullOrEmpty(nextNeighbour.Value.lyric)
                && nextNeighbour.Value.lyric != "+";

            CVnCRule prevRule = hasPrev
                ? (rules.TryGetValue(prevNeighbour!.Value.lyric!, out var pr) ? pr : new CVnCRule())
                : new CVnCRule();

            var phonemes = new List<Phoneme>();

            // 1. 开头 / 连接
            if (!hasPrev)
            {
                ProcessOnset(phonemes, currentRule, note, attr0, attr1, totalDuration, endTick);
            }
            else
            {
                ProcessContinuation(phonemes, currentRule, prevRule, note, attr0, totalDuration, endTick, prevNeighbour);
            }

            // 2. 过渡音素
            ProcessTransitions(phonemes, currentRule, note, attr0, totalDuration, hasNext);

            // 3. 尾音到静音（仅最后一个音符）
            if (!hasNext && !string.IsNullOrEmpty(currentRule.Ending))
            {
                ProcessEnding(phonemes, currentRule, note, attr0, totalDuration);
            }

            return new Result { phonemes = phonemes.ToArray() };
        }

        // -------- 开头音 --------
        private void ProcessOnset(
            List<Phoneme> phonemes, CVnCRule rule, Note note,
            PhonemeAttributes attr, PhonemeAttributes attr1,
            int totalDuration, int endTick)
        {
            // 1. 有辅音：先尝试 "- 辅音" -> 加 VC + Main
            if (!string.IsNullOrEmpty(rule.Prefix))
            {
                var vcCandidates = new List<string> { $"- {rule.Prefix}" };
                if (singer != null && CheckOtoUntilHit(vcCandidates, note, attr, out var onsetOto))
                {
                    // 取 VC 的 Preutter 与 main 的预发声到右边界的平均值
                    int vcVoice = (int)(-onsetOto.Cutoff - onsetOto.Preutter);
                    if (vcVoice <= 0) vcVoice = 60;
                    int mainMs = GetVoiceLength(rule.Main, note, attr);
                    int avgMs = (vcVoice + mainMs) / 2;
                    // 不能超过 VC 自身的真实长度
                    if (avgMs > vcVoice) avgMs = vcVoice;
                    int vcLength = -timeAxis.MsToTickAt(-avgMs, endTick);

                    try
                    {
                        double stretch = attr1.consonantStretchRatio ?? 1;
                        vcLength = (int)(vcLength * stretch);
                    }
                    catch { }

                    vcLength = Math.Max(20, vcLength);

                    phonemes.Add(new Phoneme
                    {
                        phoneme = AppendVoiceColor(onsetOto.Alias, attr),
                        position = -vcLength,
                    });

                    AddMainPhoneme(phonemes, rule.Main, note, attr, 0);
                    return;
                }
            }

            // 2. 尝试 "- 主音素"（替代整个音节，不再加 Main）
            var altCandidates = new List<string> { $"- {rule.Main}" };
            if (singer != null && CheckOtoUntilHit(altCandidates, note, attr, out var altOto))
            {
                phonemes.Add(new Phoneme
                {
                    phoneme = AppendVoiceColor(altOto.Alias, attr),
                    position = 0,
                });
                return;
            }

            // 3. 最后只用 Main
            AddMainPhoneme(phonemes, rule.Main, note, attr, 0);
        }

        // -------- 非开头音 --------
        private void ProcessContinuation(
            List<Phoneme> phonemes, CVnCRule rule, CVnCRule prevRule,
            Note note, PhonemeAttributes attr,
            int totalDuration, int endTick,
            Note? prevNeighbour)

        {
            string prevEnding = prevRule?.Ending ?? "";

            if (!string.IsNullOrEmpty(rule.Prefix))
            {
                // ncv 优先（前尾音 + 当前整音，如 "n~ zA"）
                if (!string.IsNullOrEmpty(prevEnding))
                {
                    var ncvCandidates = new List<string> { $"{prevEnding} {rule.Main}" };
                    if (singer != null && CheckOtoUntilHit(ncvCandidates, note, attr, out var ncvOto))
                    {
                        phonemes.Add(new Phoneme
                        {
                            phoneme = AppendVoiceColor(ncvOto.Alias, attr),
                            position = 0,
                        });
                        return;
                    }
                }
                // 有辅音： "prevEnding prefix" -> "prefix" -> "main"
                var candidates = new List<string>();
                if (!string.IsNullOrEmpty(prevEnding))
                {
                    candidates.Add($"{prevEnding} {rule.Prefix}");
                }
                candidates.Add(rule.Prefix);
                candidates.Add(rule.Main);

                if (singer != null && CheckOtoUntilHit(candidates, note, attr, out var vcOto))
                {
                    int vcLength = ComputeVcLength(rule.Main, note, attr, totalDuration, endTick);

                    if (!string.IsNullOrEmpty(prevEnding) && singer != null &&
                           singer.TryGetMappedOto(prevEnding, note.tone + attr.toneShift, attr.voiceColor, out var prevEndOto))
                    {
                        int prevEndVoice = (int)(-prevEndOto.Cutoff - prevEndOto.Preutter);
                        int prevOverlap = (int)prevEndOto.Overlap;
                        if (prevOverlap < 0) prevOverlap = 0;

                        // 合并跨度 = VC + 前尾音 - 重叠量，再取一半
                        int combined = vcLength + prevEndVoice - prevOverlap;
                        if (combined > 0)
                            vcLength = combined * 2 / 3;
                    }

                    // 用实际跨度（含 "+"）而不是 raw duration
                    if (prevNeighbour.HasValue)
                    {
                        var pn = prevNeighbour.Value;
                        int prevSpan = Math.Max(pn.duration, note.position - pn.position);

                        // 上限 = 前音符跨度的 2/3（宽松，避免因前音符太短就被砍）
                        int upper = Math.Max(10, prevSpan / 3);   // 上限 1/3
                        vcLength = Math.Min(vcLength, upper);

                        // 空隙避让：VC 不能侵占到前音符内部
                        int gap = note.position - (pn.position + pn.duration);
                        if (gap > 0)
                        {
                            vcLength = Math.Min(vcLength, gap);
                        }
                        // gap <= 0 说明音符紧贴或重叠，不限制，让 OpenUtau 引擎自己处理
                        // VC 上限 = 自身 Preutter
                        int vcPre = (int)vcOto.Preutter;
                        if (vcPre > 0 && vcLength > vcPre)
                            vcLength = vcPre;
                        if (vcLength < 10) vcLength = 10;
                    }

                    phonemes.Add(new Phoneme
                    {
                        phoneme = AppendVoiceColor(vcOto.Alias, attr),
                        position = -vcLength,
                    });
                }   

                AddMainPhoneme(phonemes, rule.Main, note, attr, 0);   // ← 在 if 外面
            }
            else
            {
                // 无辅音： "prevEnding main" -> "main"
                var candidates = new List<string>();
                if (!string.IsNullOrEmpty(prevEnding))
                {
                    candidates.Add($"{prevEnding} {rule.Main}");
                }
                candidates.Add(rule.Main);

                if (singer != null && CheckOtoUntilHit(candidates, note, attr, out var oto))
                {
                    phonemes.Add(new Phoneme
                    {
                        phoneme = AppendVoiceColor(oto.Alias, attr),
                        position = 0,
                    });
                }
                else
                {
                    AddMainPhoneme(phonemes, rule.Main, note, attr, 0);
                }
            }
        }


        private int ComputeVcLength(string mainPhoneme, Note note, PhonemeAttributes attr, int totalDuration, int endTick)
        {
            if (singer != null &&
                singer.TryGetMappedOto(mainPhoneme, note.tone + attr.toneShift, attr.voiceColor, out var oto))
            {
                int vcLength = -timeAxis.MsToTickAt(-oto.Preutter, endTick);
                return Math.Max(30, vcLength);   // 不再被 T/3 砍
            }
            return 30;
        }

        // 预发声到右边界
        private int GetVoiceLength(string phoneme, Note note, PhonemeAttributes attr)
        {
            if (singer != null &&
                singer.TryGetMappedOto(phoneme, note.tone + attr.toneShift, attr.voiceColor, out var oto))
            {
                int dur = (int)(-oto.Cutoff - oto.Preutter);   // ← 改这里
                if (dur <= 0) dur = 60;
                return dur;
            }
            return 60;
        }
        private int GetFixedDuration(string phoneme, Note note, PhonemeAttributes attr)
        {
            if (singer != null &&
                singer.TryGetMappedOto(phoneme, note.tone + attr.toneShift, attr.voiceColor, out var oto))
            {
                int voiceLen = (int)(-oto.Cutoff - oto.Preutter);
                int consonant = (int)oto.Consonant;

                // 韵尾衔接部（以 "_" 开头）：用预发声到右边界，不用平均
                if (phoneme.StartsWith("_"))
                {
                    if (voiceLen <= 0) voiceLen = 60;
                    return voiceLen;
                }

                // 其他：用平均
                if (voiceLen <= 0 && consonant <= 0) return 60;
                if (voiceLen <= 0) return consonant;
                if (consonant <= 0) return voiceLen;
                return (voiceLen + consonant) / 2;
            }
            return 60;
        }

        private string ResolveAlias(string phoneme, Note note, PhonemeAttributes attr)
        {
            if (singer != null &&
                singer.TryGetMappedOto(phoneme, note.tone + attr.toneShift, attr.voiceColor, out var oto))
            {
                return oto.Alias;
            }
            return phoneme;
        }

        // -------- 过渡音素（缩短等比、拉长只拉拉伸点）--------
        private void ProcessTransitions(
            List<Phoneme> phonemes, CVnCRule rule, Note note,
            PhonemeAttributes attr, int totalDuration, bool hasNext)
        {
            // 1. 收集有效过渡音素
            var transitions = new List<(string resolved, int rawDur, bool isMedial)>();
            foreach (var t in rule.TransitionPhonemes)
            {
                if (string.IsNullOrEmpty(t)) continue;
                if (singer == null) continue;

                bool exists = CheckOtoExists(t, note.tone, attr)
                           || CheckOtoUntilHit(new List<string> { t }, note, attr, out _);
                if (!exists) continue;
                // 只有明确"以 _ 结尾、不以 _ 开头"才是介母衔接部（拉伸）
                // 以 _ 开头的一律是韵尾衔接部（固定），避免 _a't、_ut 被误判
                bool isMedial = t.EndsWith("_") && !t.StartsWith("_");
                string resolved = ResolveAlias(t, note, attr);
                int rawDur = GetFixedDuration(t, note, attr);
                transitions.Add((resolved, rawDur, isMedial));
            }

            // 2. 尾音预留
            int endingReserve = 0;
            if (!hasNext && !string.IsNullOrEmpty(rule.Ending))
                endingReserve = Math.Max(20, Math.Min(totalDuration / 6, 60));

            // 3. main 固定时长
            bool mainIsFixed = rule.Main.EndsWith("_");
            int mainRaw = 0;
            if (mainIsFixed)
            {
                // main 的介母长度 = Preutter
                int a = 0;
                if (singer != null && singer.TryGetMappedOto(rule.Main,
                        note.tone + attr.toneShift, attr.voiceColor, out var mainOto))
                {
                    a = (int)mainOto.Preutter;
                    if (a < 0) a = 0;
                }

                // med 的介母长度 = Preutter；medOverlap 最多抵消到 Preutter
                int b = 0;
                int medOverlap = 0;
                if (singer != null)
                {
                    foreach (var t in rule.TransitionPhonemes)
                    {
                        if (string.IsNullOrEmpty(t)) continue;
                        if (!t.EndsWith("_") || t.StartsWith("_")) continue;
                        if (!CheckOtoExists(t, note.tone, attr)
                            && !CheckOtoUntilHit(new List<string> { t }, note, attr, out _)) continue;

                        if (singer.TryGetMappedOto(t, note.tone + attr.toneShift, attr.voiceColor, out var otoM))
                        {
                            b = (int)otoM.Preutter;
                            if (b < 0) b = 0;

                            int mo = (int)otoM.Overlap;
                            if (mo < 0) mo = 0;
                            medOverlap = Math.Min(mo, b);   // 溢出红线的不算
                            break;
                        }
                    }
                }

                if (a > 0 && b > 0)
                    mainRaw = (a + b - medOverlap) / 2;
                else if (a > 0)
                    mainRaw = a;
                else if (b > 0)
                    mainRaw = b;
                else
                    mainRaw = GetVoiceLength(rule.Main, note, attr);

                int minMain = Math.Max(20, totalDuration / 10);
                if (mainRaw < minMain) mainRaw = minMain;
                if (mainRaw > totalDuration / 3) mainRaw = totalDuration / 3;
            }

            // 4. 介母期望时长 = Preutter
            int[] medialExpected = new int[transitions.Count];
            for (int i = 0; i < transitions.Count; i++)
            {
                if (!transitions[i].isMedial) continue;

                if (singer != null && singer.TryGetMappedOto(transitions[i].resolved,
                        note.tone + attr.toneShift, attr.voiceColor, out var otoSelf))
                {
                    int pre = (int)otoSelf.Preutter;
                    if (pre < 20) pre = 20;
                    medialExpected[i] = pre;
                }
                else
                {
                    medialExpected[i] = 20;
                }
            }

            // 5. 分配实际时长
            int[] actualTrans = new int[transitions.Count];
            int actualEnding = endingReserve;

            // 计算固定部总和 & 拉伸部总和
            int fixedSum = endingReserve;
            if (mainIsFixed) fixedSum += mainRaw;
            int stretchSum = 0;
            if (!mainIsFixed) stretchSum += mainRaw;
            int stretchCount = mainIsFixed ? 0 : 1;
            foreach (var t in transitions)
            {
                if (t.isMedial) { stretchSum += t.rawDur; }   // 只累积，不算拉伸点
                else fixedSum += t.rawDur;
            }

            int minVowelSpace = Math.Max(60, totalDuration / 5);   // 元音至少占 1/5

            if (totalDuration >= fixedSum + minVowelSpace)
            {
                // ---- 空间够：固定部不动，拉伸部按期望时长分配 ----
                int remaining = totalDuration - fixedSum;
                int perStretch = stretchCount > 0 ? Math.Max(1, remaining / stretchCount) : 0;

                for (int i = 0; i < transitions.Count; i++)
                {
                    if (transitions[i].isMedial)
                    {
                        actualTrans[i] = medialExpected[i];   // 直接用期望值，不跟 main 平分
                    }
                    else
                    {
                        actualTrans[i] = transitions[i].rawDur;
                    }
                }
            }
            else
            {
                // ---- 空间不够：所有音素按同一个比例缩 ----
                int totalRaw = fixedSum + stretchSum;
                double scale = totalRaw > 0 ? (double)totalDuration / totalRaw : 1;
                if (scale > 1) scale = 1;

                actualEnding = Math.Max(1, (int)(endingReserve * scale));

                for (int i = 0; i < transitions.Count; i++)
                {
                    actualTrans[i] = Math.Max(1, (int)(transitions[i].rawDur * scale));
                }

                if (mainIsFixed)
                {
                    mainRaw = Math.Max(1, (int)(mainRaw * scale));
                }
            }

            // 6. 从右往左布局
            int minMain2 = Math.Max(20, totalDuration / 10);
            int cursor = totalDuration - actualEnding;
            if (cursor < 0) cursor = 0;

            var positions = new int[transitions.Count];
            for (int i = transitions.Count - 1; i >= 0; i--)
            {
                cursor -= actualTrans[i];
                if (cursor < minMain2) cursor = minMain2;   // ← 不许越过 main 保底位
                positions[i] = cursor;
            }

            // 6b. 介母：紧贴 main 右边缘，不再从右边缘往回推
            int mainRight = mainIsFixed ? mainRaw : 0;

            for (int i = 0; i < transitions.Count; i++)
            {
                if (!transitions[i].isMedial) continue;

                // 直接放在 main 右边缘
                int newPos = mainRight;
                if (newPos < 0) newPos = 0;

                // 防止越过下一锚点
                int nextAnchor = (i + 1 < transitions.Count)
                    ? positions[i + 1]
                    : totalDuration - actualEnding;

                if (newPos >= nextAnchor)
                    newPos = Math.Max(0, nextAnchor - 10);

                positions[i] = newPos;
            }

            // 7. 添加音素
            for (int i = 0; i < transitions.Count; i++)
            {
                phonemes.Add(new Phoneme
                {
                    phoneme = AppendVoiceColor(transitions[i].resolved, attr),
                    position = positions[i],
                });
            }
        }

        // -------- 尾音到静音 --------
        private void ProcessEnding(
            List<Phoneme> phonemes, CVnCRule rule, Note note,
            PhonemeAttributes attr, int totalDuration)
        {
            string ending = $"{rule.Ending} R";
            if (singer == null || !CheckOtoExists(ending, note.tone, attr))
            {
                ending = $"{rule.Ending} -";
            }

            int endingLength = Math.Min(totalDuration / 6, 60);
            int endingPosition = Math.Max(0, totalDuration - endingLength);

            if (singer != null && CheckOtoUntilHit(new List<string> { ending }, note, attr, out var oto))
            {
                phonemes.Add(new Phoneme
                {
                    phoneme = AppendVoiceColor(oto.Alias, attr),
                    position = endingPosition,
                });
            }
            else if (singer != null && CheckOtoExists(ending, note.tone, attr))
            {
                phonemes.Add(new Phoneme
                {
                    phoneme = AppendVoiceColor(ending, attr),
                    position = endingPosition,
                });
            }
        }

        // -------- 辅助方法 --------
        private static PhonemeAttributes GetAttr(Note note, int index)
        {
            if (note.phonemeAttributes != null)
            {
                foreach (var a in note.phonemeAttributes)
                {
                    if (a.index == index)
                    {
                        return a;
                    }
                }
            }
            return new PhonemeAttributes { index = index };
        }

        private void AddMainPhoneme(
            List<Phoneme> phonemes, string mainPhoneme, Note note,
            PhonemeAttributes attr, int position)
        {
            if (string.IsNullOrEmpty(mainPhoneme))
            {
                return;
            }

            if (singer != null && CheckOtoUntilHit(new List<string> { mainPhoneme }, note, attr, out var oto))
            {
                phonemes.Add(new Phoneme
                {
                    phoneme = AppendVoiceColor(oto.Alias, attr),
                    position = position,
                });
            }
            else
            {
                phonemes.Add(new Phoneme
                {
                    phoneme = AppendVoiceColor(mainPhoneme, attr),
                    position = position,
                });
            }
        }

        private bool CheckOtoUntilHit(
            List<string> candidates, Note note, PhonemeAttributes attr, out UOto oto)
        {
            oto = default;
            if (singer == null)
            {
                return false;
            }

            foreach (string test in candidates)
            {
                if (string.IsNullOrEmpty(test))
                {
                    continue;
                }

                if (singer.TryGetMappedOto(test, note.tone + attr.toneShift, attr.voiceColor, out var result))
                {
                    oto = result;
                    return true;
                }
            }

            return false;
        }

        private bool CheckOtoExists(string phoneme, int tone, PhonemeAttributes attr)
        {
            if (singer == null || string.IsNullOrEmpty(phoneme))
            {
                return false;
            }
            return singer.TryGetMappedOto(phoneme, tone + attr.toneShift, attr.voiceColor, out _);
        }

        private static string AppendVoiceColor(string phoneme, PhonemeAttributes attr)
        {
            if (string.IsNullOrEmpty(phoneme))
            {
                return "";
            }

            string color = attr.voiceColor ?? "";
            if (string.IsNullOrEmpty(color))
            {
                return phoneme;
            }

            // 只有当音素本身没有以该音色结尾时才追加，避免重复
            if (phoneme.EndsWith(" " + color))
            {
                return phoneme;
            }

            return $"{phoneme} {color}";
        }

        private static Result MakeSimpleResult(string phoneme, PhonemeAttributes attr)
        {
            if (string.IsNullOrEmpty(phoneme))
            {
                phoneme = "-";
            }

            return new Result
            {
                phonemes = new[]
                {
                    new Phoneme { phoneme = AppendVoiceColor(phoneme, attr) }
                },
            };
        }

        private class CVnCRule
        {
            public string Prefix { get; set; } = "";
            public string Main { get; set; } = "";
            public List<string> TransitionPhonemes { get; set; } = new();
            public string Ending { get; set; } = "";
        }
    }
}