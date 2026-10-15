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
                    int vcVoice = (int)(-onsetOto.Cutoff - onsetOto.Preutter);
                    if (vcVoice <= 0) vcVoice = 60;
                    int mainMs = GetVoiceLength(rule.Main, note, attr);
                    int avgMs = (vcVoice + mainMs) / 2;
                    if (avgMs > vcVoice) avgMs = vcVoice;
                    int vcLength = -timeAxis.MsToTickAt(-avgMs, endTick);

                    try { vcLength = (int)(vcLength * (attr1.consonantStretchRatio ?? 1)); } catch { }
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
                // ncv 优先
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

                var vcCandidates = new List<string>();
                if (!string.IsNullOrEmpty(prevEnding))
                    vcCandidates.Add($"{prevEnding} {rule.Prefix}");
                vcCandidates.Add(rule.Prefix);
                vcCandidates.Add(rule.Main);

                if (singer != null && CheckOtoUntilHit(vcCandidates, note, attr, out var vcOto))
                {
                    UOto mainOtoForVc = default;
                    singer?.TryGetMappedOto(rule.Main, note.tone + attr.toneShift, attr.voiceColor, out mainOtoForVc);
                    int vcLength = ComputeVcLength(vcOto, mainOtoForVc, endTick);
                    int mainOverlap = GetMainOverlap(rule.Main, note, attr, endTick);

                    // 上限 1：VC 自身物理辅音长度（软削）
                    int vcVoiceMax = (int)(-vcOto.Cutoff - vcOto.Preutter);
                    if (vcVoiceMax > 0 && vcLength > vcVoiceMax)
                        vcLength = (vcLength + vcVoiceMax) / 2;

                    // 上限 2：不侵占前一个音符超过 1/3（软削）
                    if (prevNeighbour.HasValue)
                    {
                        var pn = prevNeighbour.Value;
                        int prevEnd = pn.position + pn.duration;
                        int gap = Math.Max(0, note.position - prevEnd);
                        int maxOverrun = pn.duration / 3;
                        int cap = gap + maxOverrun + mainOverlap;
                        if (cap < 20) cap = 20;
                        if (vcLength > cap)
                            vcLength = (vcLength + cap) / 2;
                    }

                    if (vcLength < 10) vcLength = 10;
                    int vcPosition = -vcLength + mainOverlap;

                    phonemes.Add(new Phoneme
                    {
                        phoneme = AppendVoiceColor(vcOto.Alias, attr),
                        position = vcPosition,
                    });

                    AddMainPhoneme(phonemes, rule.Main, note, attr, 0);
                    return;
                }

                var mainCandidates = new List<string>();
                if (!string.IsNullOrEmpty(prevEnding))
                    mainCandidates.Add($"{prevEnding} {rule.Main}");
                mainCandidates.Add(rule.Main);

                if (singer != null && CheckOtoUntilHit(mainCandidates, note, attr, out var oto1))
                {
                    phonemes.Add(new Phoneme
                    {
                        phoneme = AppendVoiceColor(oto1.Alias, attr),
                        position = 0,
                    });
                }
                else
                {
                    AddMainPhoneme(phonemes, rule.Main, note, attr, 0);
                }
            }
            else
            {
                var candidates = new List<string>();
                if (!string.IsNullOrEmpty(prevEnding))
                    candidates.Add($"{prevEnding} {rule.Main}");
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

        private int ComputeVcLength(UOto vcOto, UOto mainOto, int endTick)
        {
            int vcVoice = (int)(-vcOto.Cutoff - vcOto.Preutter);
            if (vcVoice < 0) vcVoice = 0;

            int ov = (int)mainOto.Overlap;
            int ovAbs = ov < 0 ? -ov : ov;

            int pre = (int)mainOto.Preutter;

            int totalMs = (vcVoice + ovAbs) / 2 + pre;
            if (totalMs <= 0) totalMs = 30;

            return Math.Max(30, -timeAxis.MsToTickAt(-totalMs, endTick));
        }

        private int GetMainOverlap(string mainPhoneme, Note note, PhonemeAttributes attr, int endTick)
        {
            if (singer == null) return 0;
            if (!singer.TryGetMappedOto(mainPhoneme, note.tone + attr.toneShift, attr.voiceColor, out var oto))
                return 0;
            double ovMs = oto.Overlap;   // 可为负
            return -timeAxis.MsToTickAt(-ovMs, endTick);
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

        // -------- 过渡音素--------
        private void ProcessTransitions(
            List<Phoneme> phonemes, CVnCRule rule, Note note,
            PhonemeAttributes attr, int totalDuration, bool hasNext)
        {
            // ========== 1. 收集有效过渡音素 ==========
            var transitions = new List<(string resolved, int rawDur, bool isMedial)>();
            foreach (var t in rule.TransitionPhonemes)
            {
                if (string.IsNullOrEmpty(t)) continue;
                if (singer == null) continue;

                bool exists = CheckOtoExists(t, note.tone, attr)
                           || CheckOtoUntilHit(new List<string> { t }, note, attr, out _);
                if (!exists) continue;

                bool isMedial = t.EndsWith("_") && !t.StartsWith("_");
                string resolved = ResolveAlias(t, note, attr);
                int rawDur = GetFixedDuration(t, note, attr);
                transitions.Add((resolved, rawDur, isMedial));
            }

            // ========== 2. 尾音预留 ==========
            int endingReserve = 0;
            if (!hasNext && !string.IsNullOrEmpty(rule.Ending))
                endingReserve = Math.Max(20, Math.Min(totalDuration / 6, 60));

            // ========== 3. main 原始长度 ==========
            bool mainIsFixed = rule.Main.EndsWith("_");
            int mainRaw;

            UOto mainOto = default;
            bool hasMainOto = singer != null && singer.TryGetMappedOto(
                rule.Main, note.tone + attr.toneShift, attr.voiceColor, out mainOto);

            if (mainIsFixed)
            {
                int a = 0;
                if (hasMainOto)
                {
                    a = (int)(-mainOto.Cutoff - mainOto.Preutter);
                    if (a < 0) a = 0;
                }
                int b = 0;
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
                            break;
                        }
                    }
                }
                if (a > 0 && b > 0) mainRaw = (a + b) / 2;
                else if (a > 0) mainRaw = a;
                else if (b > 0) mainRaw = b;
                else mainRaw = GetVoiceLength(rule.Main, note, attr);
            }
            else
            {
                mainRaw = GetVoiceLength(rule.Main, note, attr);
            }
            if (mainRaw < 20) mainRaw = 20;

            // ========== 4. main 拆辅音 / 元音（元音至少 1/3）==========
            int mainCons = 0;
            if (hasMainOto)
            {
                int c = (int)mainOto.Consonant;
                int p = Math.Max(0, (int)mainOto.Preutter);
                if (c > 0 && c <= mainRaw) mainCons = c;
                else mainCons = (int)(p * 0.7);
            }

            // main 辅音最多占 mainRaw 的 2/3，保证元音至少 1/3
            int maxMainCons = mainRaw * 2 / 3;
            if (mainCons > maxMainCons) mainCons = maxMainCons;
            if (mainCons < 0) mainCons = 0;
            int mainVowel = Math.Max(0, mainRaw - mainCons);

            // 韵尾等非介母（整体归衔接段）
            int tailCons = 0;
            foreach (var t in transitions)
                if (!t.isMedial) tailCons += t.rawDur;

            // ========== 5. 介母拆辅音 / 元音（同样给元音 1/3 保护）==========
            int[] medCons = new int[transitions.Count];
            int[] medVowel = new int[transitions.Count];
            for (int i = 0; i < transitions.Count; i++)
            {
                if (!transitions[i].isMedial) continue;
                if (singer != null && singer.TryGetMappedOto(transitions[i].resolved,
                        note.tone + attr.toneShift, attr.voiceColor, out var otoSelf))
                {
                    int pre = (int)otoSelf.Preutter;
                    int ov = (int)otoSelf.Overlap;
                    if (ov < 0) ov = 0;
                    int expected = Math.Max(pre, ov);
                    if (expected < 20) expected = 20;

                    int prot = pre < expected ? pre : expected;
                    if (prot < 20) prot = 20;
                    if (prot > expected) prot = expected;

                    // 介母辅音也不超过期望的 2/3
                    int maxProt = expected * 2 / 3;
                    if (prot > maxProt) prot = maxProt;

                    medCons[i] = prot;
                    medVowel[i] = expected - prot;
                }
                else
                {
                    medCons[i] = 20;
                    medVowel[i] = 0;
                }
            }

            // ========== 6. 缩放：元音锁 T/3，剩余按 S2:S3 需求比例 ==========
            int s1 = mainVowel;
            foreach (var v in medVowel) s1 += v;

            int s2 = tailCons + endingReserve;
            foreach (var c in medCons) s2 += c;

            int s3 = mainCons;

            double vScale = 1.0;
            double tScale = 1.0;
            double mScale = 1.0;

            int totalNeeded = s1 + s2 + s3;
            if (totalNeeded > totalDuration)
            {
                // 元音下限
                int minVowel = totalDuration / 2;      // 元音至少 50%

                int actualVowel;
                if (s1 <= minVowel)
                {
                    // 元音需求本来就不到 T/3 → 全保，不拉长
                    actualVowel = s1;
                    vScale = 1.0;
                }
                else
                {
                    // 元音需求大于 T/3 → 缩到 T/3
                    actualVowel = minVowel;
                    vScale = (double)actualVowel / s1;
                }

                int remaining = totalDuration - actualVowel;
                if (remaining < 0) remaining = 0;

                int s23 = s2 + s3;
                if (s23 > 0)
                {
                    if (remaining >= s23)
                    {
                        tScale = 1.0;
                        mScale = 1.0;
                    }
                    else
                    {
                        // 按需求比例分剩余
                        int roomT = (int)((long)remaining * s2 / s23);
                        int roomM = remaining - roomT;
                        tScale = s2 > 0 ? Math.Min(1.0, (double)roomT / s2) : 1.0;
                        mScale = s3 > 0 ? Math.Min(1.0, (double)roomM / s3) : 1.0;
                    }
                }
            }

            Log.Warning($"[LAYOUT] MAIN={rule.Main} T={totalDuration} mainRaw={mainRaw} " +
                        $"mainCons={mainCons} mainVowel={mainVowel} tailCons={tailCons} endingReserve={endingReserve} " +
                        $"S1={s1} S2={s2} S3={s3} vScale={vScale:F3} tScale={tScale:F3} mScale={mScale:F3}");

            // ========== 7. 计算实际长度 ==========
            int actualCons = (int)(mainCons * mScale);
            // 辅音视觉上限 = 元音视觉长度的 1.5 倍（可调）
            int actualVowelTmp = (int)(mainVowel * vScale);
            int consCap = actualVowelTmp * 3 / 2;
            if (consCap < 20) consCap = 20;
            if (actualCons > consCap)
            {
                // 省下的 tick 转给元音
                int surplus = actualCons - consCap;
                actualCons = consCap;
                actualVowelTmp += surplus;
            }
            int actualMain = actualCons + actualVowelTmp;
            if (actualMain < 1) actualMain = 1;

            int actualEnding = Math.Max(1, (int)(endingReserve * tScale));

            int[] actualTrans = new int[transitions.Count];
            for (int i = 0; i < transitions.Count; i++)
            {
                if (transitions[i].isMedial)
                {
                    int c = (int)(medCons[i] * tScale);
                    int v = (int)(medVowel[i] * vScale);
                    actualTrans[i] = Math.Max(1, c + v);
                }
                else
                {
                    actualTrans[i] = Math.Max(1, (int)(transitions[i].rawDur * tScale));
                }
            }

            // ========== 8. 布局 ==========
            var positions = new int[transitions.Count];
            var placed = new bool[transitions.Count];

            int cursor = totalDuration - actualEnding;
            if (cursor < 0) cursor = 0;
            for (int i = transitions.Count - 1; i >= 0; i--)
            {
                if (transitions[i].isMedial) continue;
                cursor -= actualTrans[i];
                if (cursor < 0) cursor = 0;
                positions[i] = cursor;
                placed[i] = true;
            }

            for (int i = 0; i < transitions.Count; i++)
            {
                if (!transitions[i].isMedial) continue;
                int newPos = actualMain;
                if (newPos < 0) newPos = 0;
                int nextAnchor = totalDuration - actualEnding;
                for (int j = i + 1; j < transitions.Count; j++)
                {
                    if (placed[j]) { nextAnchor = positions[j]; break; }
                }
                if (newPos >= nextAnchor) newPos = Math.Max(0, nextAnchor - 1);
                positions[i] = newPos;
                placed[i] = true;
            }

            // ========== 9. 添加音素 ==========
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