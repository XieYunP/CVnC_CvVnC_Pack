using OpenUtau.Api;
using OpenUtau.Core.Format.MusicXMLSchema;
using OpenUtau.Core.Ustx;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            // 计算当前音符和前一个音符的物理时长（毫秒）
            int durationMs = (int)(timeAxis.TickPosToMsPos(endTick) - timeAxis.TickPosToMsPos(note.position));
            int prevDur = prevNeighbour?.duration ?? 0;
            int prevStartTick = prevNeighbour?.position ?? note.position;
            int prevEndTick = prevStartTick + prevDur;
            int prevDurMs = prevNeighbour.HasValue ? (int)(timeAxis.TickPosToMsPos(prevEndTick) - timeAxis.TickPosToMsPos(prevStartTick)) : 0;

            // 找规则
            if (!rules.TryGetValue(lyric, out var currentRule))
            {
                return MakeSimpleResult(lyric, attr0);
            }

            // 前后邻居信息
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
                ProcessContinuation(phonemes, currentRule, prevRule, note, attr0, totalDuration, endTick, prevNeighbour, durationMs, prevDurMs);
            }

            // 2. 过渡音素（包含尾音）
            ProcessTransitions(phonemes, currentRule, prevRule, note, attr0, totalDuration, hasNext, hasPrev, durationMs, prevDurMs, nextNeighbour, endTick);

            // ========== 终极防重叠（距离不足强行推开，推不开就删） ==========
            phonemes = phonemes.OrderBy(p => p.position).ToList();
            var cleaned = new List<Phoneme>();
            int lastPos = -99999;

            foreach (var p in phonemes)
            {
                if (cleaned.Count == 0) { cleaned.Add(p); lastPos = p.position; continue; }

                if (p.position - lastPos < 4)
                {
                    int newPos = lastPos + 4;
                    if (newPos < totalDuration)
                    {
                        var temp = p;
                        temp.position = newPos;
                        cleaned.Add(temp);
                        lastPos = newPos;
                    }
                }
                else
                {
                    if (p.position >= totalDuration) continue;
                    cleaned.Add(p);
                    lastPos = p.position;
                }
            }
            phonemes = cleaned;

            return new Result { phonemes = phonemes.ToArray() };
        }

        // -------- 开头音 --------
        private void ProcessOnset(
            List<Phoneme> phonemes, CVnCRule rule, Note note,
            PhonemeAttributes attr, PhonemeAttributes attr1,
            int totalDuration, int endTick)
        {
            if (!string.IsNullOrEmpty(rule.Prefix))
            {
                var vcCandidates = new List<string> { $"- {rule.Prefix}" };
                if (singer != null && CheckOtoUntilHit(vcCandidates, note, attr, out var onsetOto))
                {
                    int vcVoiceMs = (int)(-onsetOto.Cutoff - onsetOto.Preutter);
                    if (vcVoiceMs <= 0) vcVoiceMs = 60;

                    int mainPreMs = 0;
                    int mainOvMsSigned = 0;
                    if (singer.TryGetMappedOto(rule.Main,
                            note.tone + attr.toneShift, attr.voiceColor, out var mainOto))
                    {
                        mainPreMs = (int)mainOto.Preutter;
                        if (mainPreMs < 0) mainPreMs = 0;
                        mainOvMsSigned = (int)mainOto.Overlap;
                    }

                    // 【核心修复】将重叠钳制在预发声内。如果重叠晚于预发声，多出的部分属于元音区，不应被 VC 保护
                    int clampedOvAbsMs = Math.Min(Math.Abs(mainOvMsSigned), mainPreMs);
                    int mainConsPartMs = mainPreMs - clampedOvAbsMs;

                    int vcLengthMs = (clampedOvAbsMs + vcVoiceMs) / 2;
                    try { vcLengthMs = (int)(vcLengthMs * (attr1.consonantStretchRatio ?? 1)); } catch { }
                    if (vcLengthMs < 20) vcLengthMs = 20;
                    int vcLengthTick = -timeAxis.MsToTickAt(-vcLengthMs, endTick);

                    int vcPosTick;
                    int mainPosTick = 0;
                    if (mainOvMsSigned >= 0)
                    {
                        vcPosTick = -vcLengthTick + (-timeAxis.MsToTickAt(-clampedOvAbsMs, endTick));
                    }
                    else
                    {
                        int denom = mainPreMs + clampedOvAbsMs;
                        if (denom > 0)
                        {
                            int mainShiftMs = clampedOvAbsMs * mainPreMs / denom;
                            int vcShiftMs = clampedOvAbsMs * clampedOvAbsMs / denom;
                            mainPosTick = -timeAxis.MsToTickAt(-mainShiftMs, endTick);
                            vcPosTick = -vcLengthTick + (-timeAxis.MsToTickAt(-vcShiftMs, endTick));
                        }
                        else
                        {
                            mainPosTick = 0;
                            vcPosTick = -vcLengthTick;
                        }
                    }

                    if (vcPosTick + vcLengthTick > mainPosTick)
                    {
                        vcPosTick = mainPosTick - vcLengthTick;
                    }

                    phonemes.Add(new Phoneme
                    {
                        phoneme = AppendVoiceColor(onsetOto.Alias, attr),
                        position = vcPosTick,
                    });

                    AddMainPhoneme(phonemes, rule.Main, note, attr, mainPosTick);
                    return;
                }
            }

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

            AddMainPhoneme(phonemes, rule.Main, note, attr, 0);
        }

        // -------- 非开头音 --------
        private void ProcessContinuation(
            List<Phoneme> phonemes, CVnCRule rule, CVnCRule prevRule,
            Note note, PhonemeAttributes attr,
            int totalDuration, int endTick,
            Note? prevNeighbour, int durationMs, int prevDurMs)
        {
            string prevEnding = prevRule?.Ending ?? "";

            if (!string.IsNullOrEmpty(rule.Prefix))
            {
                if (durationMs <= 0) durationMs = 100;

                // 【极端短音保底】物理时长 < 90ms 时，直接剔除前置 VC
                if (durationMs < 90)
                {
                    if (!string.IsNullOrEmpty(prevEnding))
                    {
                        var ncvCandidates = new List<string> { $"{prevEnding} {rule.Main}" };
                        if (singer != null && CheckOtoUntilHit(ncvCandidates, note, attr, out var ncvOto))
                        {
                            phonemes.Add(new Phoneme { phoneme = AppendVoiceColor(ncvOto.Alias, attr), position = 0 });
                            return;
                        }
                    }
                    AddMainPhoneme(phonemes, rule.Main, note, attr, 0);
                    return;
                }
                // ncv 优先
                if (!string.IsNullOrEmpty(prevEnding))
                {
                    var ncvCandidates = new List<string> { $"{prevEnding} {rule.Main}" };
                    if (singer != null && CheckOtoUntilHit(ncvCandidates, note, attr, out var ncvOto))
                    {
                        phonemes.Add(new Phoneme { phoneme = AppendVoiceColor(ncvOto.Alias, attr), position = 0 });
                        return;
                    }
                }

                var vcCandidates = new List<string>();
                if (!string.IsNullOrEmpty(prevEnding)) vcCandidates.Add($"{prevEnding} {rule.Prefix}");
                vcCandidates.Add(rule.Prefix);
                vcCandidates.Add(rule.Main);

                if (singer != null && CheckOtoUntilHit(vcCandidates, note, attr, out var vcOto))
                {
                    UOto mainOtoForVc = default;
                    singer?.TryGetMappedOto(rule.Main, note.tone + attr.toneShift, attr.voiceColor, out mainOtoForVc);
                    int vcLength = ComputeVcLength(vcOto, mainOtoForVc, endTick);
                    int mainOverlap = GetMainOverlap(rule.Main, note, attr, endTick);

                    // 【新增】动态限制主音向左延伸的幅度，防止把 VC 拖向前一个音符太深处
                    if (mainOverlap < 0)
                    {
                        int maxAllowedOverlapMs = Math.Max(20, Math.Min(60, prevDurMs / 4));
                        int maxAllowedOverlapTick = -timeAxis.MsToTickAt(-maxAllowedOverlapMs, endTick);

                        if (-mainOverlap > maxAllowedOverlapTick)
                        {
                            mainOverlap = (mainOverlap - maxAllowedOverlapTick) / 2;
                        }
                    }

                    if (prevDurMs <= 0) prevDurMs = 100;

                    int maxVcByPrevMs = Math.Max(20, (int)(prevDurMs * 0.4));
                    int maxVcByPrev = -timeAxis.MsToTickAt(-maxVcByPrevMs, endTick);

                    int maxVcBySelfMs = Math.Max(25, (int)(durationMs * 0.5));
                    int maxVcBySelf = -timeAxis.MsToTickAt(-maxVcBySelfMs, endTick);

                    int maxVcLength = Math.Min(maxVcByPrev, maxVcBySelf);

                    if (vcLength > maxVcLength)
                    {
                        vcLength = (vcLength + maxVcLength) / 2;
                    }

                    if (vcLength < 6) vcLength = 6;

                    int vcPosition = -vcLength + mainOverlap;

                    int minVcPos = -maxVcLength;
                    if (vcPosition < minVcPos)
                    {
                        vcPosition = minVcPos;
                    }

                    // 【硬防线 1】允许伸展到交界线
                    int safeMargin = 0;
                    if (vcPosition + vcLength > -safeMargin)
                        vcPosition = -safeMargin - vcLength;

                    // 【硬防线 2】向后预测：温和后退，绝不粗暴砍断
                    if (prevNeighbour.HasValue)
                    {
                        var pn = prevNeighbour.Value;
                        int maxInvadeTick = Math.Max(6, (int)(pn.duration * 0.4));
                        int minPos = -maxInvadeTick;

                        if (vcPosition < minPos)
                        {
                            int excess = minPos - vcPosition;
                            vcLength = Math.Max(6, vcLength - (excess / 2));
                            vcPosition = -vcLength;
                        }
                    }

                    phonemes.Add(new Phoneme { phoneme = AppendVoiceColor(vcOto.Alias, attr), position = vcPosition });
                    AddMainPhoneme(phonemes, rule.Main, note, attr, 0);
                    return;
                }

                var mainCandidates = new List<string>();
                if (!string.IsNullOrEmpty(prevEnding)) mainCandidates.Add($"{prevEnding} {rule.Main}");
                mainCandidates.Add(rule.Main);

                if (singer != null && CheckOtoUntilHit(mainCandidates, note, attr, out var oto1))
                {
                    phonemes.Add(new Phoneme { phoneme = AppendVoiceColor(oto1.Alias, attr), position = 0 });
                }
                else
                {
                    AddMainPhoneme(phonemes, rule.Main, note, attr, 0);
                }
            }
            else
            {
                var candidates = new List<string>();
                if (!string.IsNullOrEmpty(prevEnding)) candidates.Add($"{prevEnding} {rule.Main}");
                candidates.Add(rule.Main);

                if (singer != null && CheckOtoUntilHit(candidates, note, attr, out var oto))
                {
                    phonemes.Add(new Phoneme { phoneme = AppendVoiceColor(oto.Alias, attr), position = 0 });
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

            // 去掉多余的 pre，防止 VC 把主音的辅音长度也算进去导致暴涨
            int totalMs = vcVoice + ovAbs / 2;
            if (totalMs <= 0) totalMs = 30;

            return Math.Max(30, -timeAxis.MsToTickAt(-totalMs, endTick));
        }

        private int GetMainOverlap(string mainPhoneme, Note note, PhonemeAttributes attr, int endTick)
        {
            if (singer == null) return 0;
            if (!singer.TryGetMappedOto(mainPhoneme, note.tone + attr.toneShift, attr.voiceColor, out var oto))
                return 0;
            double ovMs = oto.Overlap;
            return -timeAxis.MsToTickAt(-ovMs, endTick);
        }

        // 预发声到右边界
        private int GetVoiceLength(string phoneme, Note note, PhonemeAttributes attr)
        {
            if (singer != null &&
                singer.TryGetMappedOto(phoneme, note.tone + attr.toneShift, attr.voiceColor, out var oto))
            {
                int dur = (int)(-oto.Cutoff - oto.Preutter);
                if (dur <= 0) dur = 60;
                return dur;
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
            List<Phoneme> phonemes, CVnCRule rule, CVnCRule prevRule, Note note,
            PhonemeAttributes attr, int totalDuration, bool hasNext, bool hasPrev, int durationMs, int prevDurMs, Note? nextNeighbour, int endTick)
        {
            // 【前向感知】精准预估下一个音符的 VC 需求，按需让位
            int effectiveTotalDuration = totalDuration;
            if (hasNext && nextNeighbour.HasValue && !string.IsNullOrEmpty(nextNeighbour.Value.lyric))
            {
                string nextLyric = nextNeighbour.Value.lyric!;
                if (rules.TryGetValue(nextLyric, out var nextRule) && !string.IsNullOrEmpty(nextRule.Prefix))
                {
                    int nextDur = nextNeighbour.Value.duration;
                    int nextDurMs = (int)(timeAxis.TickPosToMsPos(nextNeighbour.Value.position + nextDur) - timeAxis.TickPosToMsPos(nextNeighbour.Value.position));

                    if (nextDurMs >= 60)
                    {
                        int estVcMs = 0;
                        string nextPrevEnding = rule.Ending ?? "";
                        var nextVcCandidates = new List<string>();
                        if (!string.IsNullOrEmpty(nextPrevEnding)) nextVcCandidates.Add($"{nextPrevEnding} {nextRule.Prefix}");
                        nextVcCandidates.Add(nextRule.Prefix);

                        if (singer != null && CheckOtoUntilHit(nextVcCandidates, nextNeighbour.Value, attr, out var nextVcOto))
                        {
                            estVcMs = (int)(-nextVcOto.Cutoff - nextVcOto.Preutter);
                            if (estVcMs <= 0) estVcMs = 40;
                        }
                        else
                        {
                            estVcMs = 40;
                        }

                        int maxReserveMs = Math.Min(Math.Min(estVcMs, 50), (int)(durationMs * 0.3));

                        if (durationMs - maxReserveMs > 30)
                        {
                            int reserveTick = -timeAxis.MsToTickAt(-maxReserveMs, endTick);
                            effectiveTotalDuration = Math.Max(10, totalDuration - reserveTick);
                        }
                    }
                }
            }

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

                int rawDur = 60;
                if (singer.TryGetMappedOto(t, note.tone + attr.toneShift, attr.voiceColor, out var otoT))
                {
                    int voiceLen = (int)(-otoT.Cutoff - otoT.Preutter);
                    int consonant = (int)otoT.Consonant;
                    if (t.StartsWith("_"))
                    {
                        rawDur = voiceLen > 0 ? voiceLen : 60;
                    }
                    else
                    {
                        if (voiceLen <= 0 && consonant <= 0) rawDur = 60;
                        else if (voiceLen <= 0) rawDur = consonant;
                        else if (consonant <= 0) rawDur = voiceLen;
                        else rawDur = (voiceLen + consonant) / 2;
                    }
                }
                transitions.Add((resolved, rawDur, isMedial));
            }

            int transDurationMs = durationMs;
            if (transDurationMs <= 0) transDurationMs = 100;

            // ========== 【新增】物理毫秒级 过渡音分级剔除 ==========
            if (transDurationMs < 60)
            {
                transitions.RemoveAll(t => !t.isMedial);
            }
            if (transDurationMs < 40)
            {
                transitions.Clear();
            }

            // ========== 2. 尾音预留 ==========
            int endingReserve = 0;
            if (effectiveTotalDuration <= 45)
            {
                endingReserve = 0;
            }
            else if (!hasNext && !string.IsNullOrEmpty(rule.Ending))
            {
                int endingRaw = 60;
                if (singer != null)
                {
                    var endingAliases = new List<string> { $"{rule.Ending} R", $"{rule.Ending} -", rule.Ending };
                    if (CheckOtoUntilHit(endingAliases, note, attr, out var endingOto))
                    {
                        int v = (int)(-endingOto.Cutoff - endingOto.Preutter);
                        int c = (int)endingOto.Consonant;
                        if (v > 0 && c > 0) endingRaw = (v + c) / 2;
                        else if (v > 0) endingRaw = v;
                        else if (c > 0) endingRaw = c;
                    }
                }
                int maxEnd = Math.Min(effectiveTotalDuration / 6, 60);
                if (endingRaw > maxEnd) endingRaw = maxEnd;
                if (endingRaw < 15) endingRaw = 15;
                endingReserve = endingRaw;
            }

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
                if (a > 0) mainRaw = a;
                else mainRaw = GetVoiceLength(rule.Main, note, attr);

                int maxMain = Math.Max(20, effectiveTotalDuration / 2);
                if (mainRaw > maxMain)
                {
                    mainRaw = (mainRaw + maxMain) / 2;
                }
            }
            else
            {
                mainRaw = GetVoiceLength(rule.Main, note, attr);
            }
            if (mainRaw < 20) mainRaw = 20;

            // ========== 4. main 拆辅音 / 元音 ==========
            int mainCons = 0;
            int mainOverlap = 0;
            if (hasMainOto)
            {
                int p = Math.Max(0, (int)mainOto.Preutter);
                int ov = (int)mainOto.Overlap;
                mainOverlap = ov;

                if (ov >= 0)
                {
                    int ovAbs = Math.Min(ov, p);
                    int nonOvPart = p - ovAbs;
                    mainCons = nonOvPart + ovAbs / 2;
                }
                else
                {
                    int leftExt = Math.Abs(ov);
                    mainCons = p + leftExt / 2;
                }

                int minMainCons = (int)(p * 0.5);
                if (mainCons < minMainCons) mainCons = minMainCons;
            }

            if (mainCons < 0) mainCons = 0;
            if (mainCons > mainRaw) mainCons = mainRaw;
            int mainVowel = Math.Max(0, mainRaw - mainCons);

            // ========== 5. 介母拆辅音 / 元音 ==========
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

                    int expected = (pre * 2 + ov) / 3;

                    if (expected < 20)
                    {
                        expected = Math.Max(20, pre / 2);
                    }

                    int maxMedial = (int)(transDurationMs / 3.0);

                    if (transDurationMs < 250)
                    {
                        maxMedial = (int)(transDurationMs * 0.2);
                    }

                    if (expected > maxMedial)
                    {
                        expected = (expected + maxMedial) / 2;
                    }

                    medCons[i] = expected;
                    medVowel[i] = 0;
                }
                else
                {
                    medCons[i] = 20;
                    medVowel[i] = 0;
                }
            }
            // 在第5步的 for 循环结束后，加入对非介母的软限制
            for (int i = 0; i < transitions.Count; i++)
            {
                if (transitions[i].isMedial) continue;
                int maxTail = Math.Max(15, totalDuration / 3);
                if (transitions[i].rawDur > maxTail)
                {
                    transitions[i] = (transitions[i].resolved, (transitions[i].rawDur + maxTail) / 2, transitions[i].isMedial);
                }
            }
            // ========== 5b. 收集总需求 ==========
            int totalRaw = mainCons + mainVowel + endingReserve;
            for (int i = 0; i < transitions.Count; i++)
            {
                if (transitions[i].isMedial) totalRaw += medCons[i] + medVowel[i];
                else totalRaw += transitions[i].rawDur;
            }

            // 核心防爆
            if (totalRaw > totalDuration * 3 && totalDuration > 0)
            {
                double ratio = (double)(totalDuration * 3) / totalRaw;
                mainCons = (int)(mainCons * ratio);
                mainVowel = (int)(mainVowel * ratio);
                endingReserve = (int)(endingReserve * ratio);
                for (int i = 0; i < transitions.Count; i++)
                {
                    if (transitions[i].isMedial)
                    {
                        medCons[i] = (int)(medCons[i] * ratio);
                        medVowel[i] = (int)(medVowel[i] * ratio);
                    }
                    else
                    {
                        transitions[i] = (transitions[i].resolved, (int)(transitions[i].rawDur * ratio), transitions[i].isMedial);
                    }
                }
                totalRaw = mainCons + mainVowel + endingReserve;
                for (int i = 0; i < transitions.Count; i++)
                {
                    if (transitions[i].isMedial) totalRaw += medCons[i] + medVowel[i];
                    else totalRaw += transitions[i].rawDur;
                }
            }

            // ========== 6. 全局统一权重等比缩 ==========
            double scale = 1.0;
            if (totalRaw > totalDuration && totalRaw > 0)
            {
                scale = (double)totalDuration / totalRaw;
            }
            if (scale < 0.1 && totalDuration > 5) scale = 0.1;
            if (totalRaw < totalDuration && totalRaw > 0)
            {
                scale = Math.Min(1.5, (double)totalDuration / totalRaw);
            }

            // ========== 7. 应用缩放与纯软削 ==========
            double dCons = mainCons * scale;
            double dVowel = mainVowel * scale;
            double dEnding = endingReserve * scale;

            // 【核心修复】固定主音（如 gu_）不参与放大，避免辅音霸屏挤压介母
            if (mainIsFixed && scale > 1.0)
            {
                dCons = mainCons;
                dVowel = mainVowel;
                dEnding = endingReserve;
            }

            double[] dTrans = new double[transitions.Count];
            for (int i = 0; i < transitions.Count; i++)
            {
                if (transitions[i].isMedial)
                    dTrans[i] = (medCons[i] + medVowel[i]) * scale;
                else
                    dTrans[i] = transitions[i].rawDur * scale;

                double maxTrans = Math.Max(10, totalDuration / 3.0);
                if (dTrans[i] > maxTrans) dTrans[i] = (dTrans[i] + maxTrans) / 2.0;
            }

            if (effectiveTotalDuration <= 60 && dCons > dVowel)
            {
                double newCons = (dCons + dVowel) / 2.0;
                double surplus = dCons - newCons;
                dCons = newCons;
                dVowel += surplus;
            }
            else if (effectiveTotalDuration > 60)
            {
                double maxConsAllowed = effectiveTotalDuration * 0.75;
                if (dCons > maxConsAllowed)
                {
                    double surplus = dCons - maxConsAllowed;
                    dCons = maxConsAllowed;
                    dVowel += surplus;
                }
            }

            if (effectiveTotalDuration <= 30 && dCons > dVowel)
            {
                double surplus = dCons - dVowel;
                dCons -= surplus / 2.0;
                dVowel += surplus / 2.0;
            }

            if (effectiveTotalDuration <= 30)
            {
                double minVowel = Math.Max(1.5, effectiveTotalDuration / 4.0);
                if (dVowel < minVowel)
                {
                    double shift = Math.Min(dCons, minVowel - dVowel);
                    dCons -= shift;
                    dVowel += shift;
                }
            }

            if (totalDuration <= 4)
            {
                dVowel = 1; dCons = 1; dEnding = 0;
            }
            else
            {
                if (dVowel < 0.5 && mainVowel > 0) dVowel = 1;
                if (dCons < 0.5 && mainCons > 0) dCons = 1;
            }

            // ========== 8. 终极防溢出 ==========
            double currentTotal = dCons + dVowel + dEnding;
            for (int i = 0; i < transitions.Count; i++) currentTotal += dTrans[i];

            if (currentTotal > totalDuration)
            {
                double excess = currentTotal - totalDuration;
                if (dEnding > 0)
                {
                    double cut = Math.Min(dEnding, excess);
                    dEnding -= cut;
                    excess -= cut;
                }
                for (int i = transitions.Count - 1; i >= 0 && excess > 0; i--)
                {
                    double cut = Math.Min(dTrans[i], excess);
                    dTrans[i] -= cut;
                    excess -= cut;
                }
                if (excess > 0)
                {
                    double cut = Math.Min(dVowel, excess);
                    dVowel -= cut;
                    excess -= cut;
                }
            }

            // ========== 8b. 转换整数并终极对齐 ==========
            int actualMain = Math.Max(1, (int)Math.Round(dCons + dVowel));
            int actualCons = Math.Max(0, (int)Math.Round(dCons));
            int actualVowel = Math.Max(0, actualMain - actualCons);
            int actualEnding = Math.Max(0, (int)Math.Round(dEnding));

            int[] actualTrans = new int[transitions.Count];
            for (int i = 0; i < transitions.Count; i++)
            {
                if (dTrans[i] < 0.8) actualTrans[i] = 0;
                else actualTrans[i] = (int)Math.Round(dTrans[i]);
            }

            int totalActual = actualMain + actualEnding;
            for (int i = 0; i < transitions.Count; i++) totalActual += actualTrans[i];

            int diff = totalDuration - totalActual;
            if (diff != 0)
            {
                if (diff > 0)
                {
                    // 空间多了，不再补给主音元音，预留给下一个音符
                }
                else
                {
                    int excess = Math.Abs(diff);
                    int cut = Math.Min(actualVowel, excess);
                    actualVowel -= cut;
                    excess -= cut;

                    for (int i = transitions.Count - 1; i >= 0 && excess > 0; i--)
                    {
                        cut = Math.Min(actualTrans[i], excess);
                        actualTrans[i] -= cut;
                        excess -= cut;
                    }

                    if (excess > 0)
                    {
                        cut = Math.Min(actualCons, excess);
                        actualCons -= cut;
                        excess -= cut;
                    }
                }
                actualMain = actualCons + actualVowel;
                if (actualMain < 1) actualMain = 1;
            }

            // ========== 9. 物理布局 ==========
            var positions = new int[transitions.Count];
            var placed = new bool[transitions.Count];

            int cursor = totalDuration - actualEnding;
            if (cursor < 0) cursor = 0;
            for (int i = transitions.Count - 1; i >= 0; i--)
            {
                if (transitions[i].isMedial) continue;
                if (actualTrans[i] <= 0) continue;
                cursor -= actualTrans[i];
                if (cursor < 0) cursor = 0;
                positions[i] = cursor;
                placed[i] = true;
            }

            for (int i = 0; i < transitions.Count; i++)
            {
                if (!transitions[i].isMedial) continue;
                if (actualTrans[i] <= 0) continue;

                int newPos = actualMain;
                if (newPos < 0) newPos = 0;
                int nextAnchor = totalDuration - actualEnding;

                for (int j = i + 1; j < transitions.Count; j++)
                {
                    if (placed[j]) { nextAnchor = positions[j]; break; }
                }

                if (newPos > nextAnchor - 4) newPos = Math.Max(0, nextAnchor - 4);
                positions[i] = newPos;
                placed[i] = true;
            }

            // ========== 10. 添加过渡音素 ==========
            for (int i = 0; i < transitions.Count; i++)
            {
                if (actualTrans[i] <= 0) continue;
                phonemes.Add(new Phoneme { phoneme = AppendVoiceColor(transitions[i].resolved, attr), position = positions[i] });
            }

            // ========== 10b. 添加尾音到静音 ==========
            if (!hasNext && !string.IsNullOrEmpty(rule.Ending) && actualEnding > 0)
            {
                string ending = $"{rule.Ending} R";
                if (singer == null || !CheckOtoExists(ending, note.tone, attr))
                {
                    ending = $"{rule.Ending} -";
                }
                int endingPosition = Math.Max(0, totalDuration - actualEnding);

                if (singer != null && CheckOtoUntilHit(new List<string> { ending }, note, attr, out var oto))
                {
                    phonemes.Add(new Phoneme { phoneme = AppendVoiceColor(oto.Alias, attr), position = endingPosition });
                }
                else if (singer != null && CheckOtoExists(ending, note.tone, attr))
                {
                    phonemes.Add(new Phoneme { phoneme = AppendVoiceColor(ending, attr), position = endingPosition });
                }
            }

            // ========== 11. 详细调试日志 ==========
            string otoInfo = hasMainOto ? $"C:{mainOto.Consonant:F1} P:{mainOto.Preutter:F1} O:{mainOto.Overlap:F1}" : "NoOto";

            string leadInfo = "无";
            if (!hasPrev)
            {
                var leadCandidates = new List<string>();
                if (!string.IsNullOrEmpty(rule.Prefix)) leadCandidates.Add($"- {rule.Prefix}");
                leadCandidates.Add($"- {rule.Main}");
                if (singer != null && CheckOtoUntilHit(leadCandidates, note, attr, out var leadOto))
                    leadInfo = $"开头[{leadOto.Alias}]";
            }
            else
            {
                var leadCandidates = new List<string>();
                if (!string.IsNullOrEmpty(prevRule?.Ending) && !string.IsNullOrEmpty(rule.Prefix))
                    leadCandidates.Add($"{prevRule.Ending} {rule.Prefix}");
                if (!string.IsNullOrEmpty(rule.Prefix))
                    leadCandidates.Add(rule.Prefix);
                if (singer != null && leadCandidates.Count > 0 && CheckOtoUntilHit(leadCandidates, note, attr, out var leadOto))
                    leadInfo = $"前导[{leadOto.Alias}]";
            }

            string prevInfo = hasPrev ? $" | 前音({prevDurMs}ms)" : "";

            Log.Warning($"\n=== [CVAL] {note.lyric} | T:{totalDuration} ({durationMs}ms){prevInfo} ===\n" +
                        $"  【主音】{rule.Main} | Oto:({otoInfo})\n" +
                        $"  【前置】{leadInfo}\n" +
                        $"  【原始】需求:{totalRaw} | 主音底长:{mainRaw} (固:{mainCons} 元:{mainVowel})\n" +
                        $"  【缩放】scale:{scale:F3} | 主音: {dCons + dVowel:F2} (固:{dCons:F2} 元:{dVowel:F2}) | 尾音:{dEnding:F2}");

            for (int i = 0; i < transitions.Count; i++)
            {
                string typeStr = transitions[i].isMedial ? "介母" : "韵尾";
                Log.Warning($"  过渡[{i}]: {transitions[i].resolved} ({typeStr}) | 计算值:{dTrans[i]:F2} -> 分配:{actualTrans[i]} | 坐标:{positions[i]}");
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