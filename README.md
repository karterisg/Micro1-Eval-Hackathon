# C# Runtime Behavior Prediction Eval

Submission for the **micro1 Eval Hackathon** — a competition to build rigorous, reproducible evaluations that expose where frontier AI models genuinely fail.

## What We Built

We designed an evaluation that asks AI models to predict the exact console output of short C# programs. Each task targets a specific C# runtime behavior that is counterintuitive even for experienced developers — cases where what the language actually does diverges from what most people expect.

Grading is binary and fully automated: the model's response must exactly match the output produced by actually compiling and running the program with .NET 10. There is no partial credit and no ambiguity. The human baseline is 100% by construction — the expected outputs are ground truth from the compiler.

## Results

| Model | Pass Rate | T02 | T11 | T13 | T16 | T19 | T23 | T26 | T28 | T31 | T33 | T35 | T42 | T45 |
|-------|:---------:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Human Expert | **100%** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| GPT-OSS 120B | **54%** | ✅ | ✅ | ✅ | ⚠️ | ❌ | ✅ | ✅ | ✅ | ❌ | ⚠️ | ❌ | ❌ | ❌ |
| Claude Sonnet 4.5 | **31%** | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ |
| Claude Haiku 4.5 | **15%** | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

✅ Pass  ⚠️ Inconsistent (passed in some trials)  ❌ Fail

## Where Each Model Fails and Why

### Claude Sonnet 4.5 — 31%

Sonnet passes tasks involving exception filter ordering (T13, T31) and iterator disposal (T35), which suggests it has solid knowledge of C# control flow. But it consistently fails on anything involving memory semantics.

**T02 — foreach closure scope:** Sonnet predicts `2 2 2` instead of `0 1 2`. It applies the pre-C#5 behavior where all loop iterations share a single variable, not the modern per-iteration copy.

**T19 — string equality through object:** Sonnet predicts `False` for the third comparison, not realizing that the specific string literal `"hello"` is interned at compile time, making the third reference check `True`.

**T23 — boxing reference equality:** Sonnet predicts `True` for `(object)42 == (object)42`. It misses that each box operation allocates a new object on the heap, making the two references distinct.

**T28 — post-increment evaluation order:** Sonnet predicts `2\n0` instead of `2\n1`. It incorrectly evaluates both uses of `i` as post-incremented rather than tracking the pre/post distinction within a single expression.

**T42 — nullable comparisons with null:** Sonnet writes `<blank line>` as text instead of outputting an actual empty line for `Console.WriteLine(null as int?)`. It understands the concept but fails the exact format requirement.

**T45 — delegate remove-last:** Sonnet predicts `21` instead of `12`. After adding `f1, f2, f1` and removing once, it believes the first `f1` is removed rather than the last, reversing the invocation order.

---

### Claude Haiku 4.5 — 15%

Haiku fails almost everything. The two tasks it passes (T19 and T26) are the ones that require knowing that reference semantics apply — which it applies correctly but doesn't generalize elsewhere.

**T13 — exception filter order:** Haiku prints `WCF D` (with a space) instead of `WCFD`. It correctly identifies which letters are printed but misunderstands the execution order, inserting output from the catch body before the `finally`.

**T16 — lambda delegate removal:** Haiku predicts `A\nC` (missing B), believing the lambda is successfully removed with `-=`. It does not know that two lambda expressions are separate objects and reference comparison always fails.

**T33 — circular static initializer:** Haiku predicts `A:0, B:0` instead of `A:2, B:1`. It cannot reason about the initialization order at all and defaults to zero for both values.

**T35 — iterator finally on early disposal:** Haiku predicts `XA1BC` instead of `XA1C`. It does not realize that `Take(1)` disposes the enumerator after the first element, causing the `finally` to run before `B` is ever yielded.

---

### GPT-OSS 120B (Groq) — 54%

GPT-OSS is the strongest model on this eval overall, but it has a distinct blind spot: it consistently fails on anything involving the interaction between exceptions, iterators, and delegates at the edge-case level.

**T16 — lambda delegate removal:** GPT-OSS correctly answers in trial 1 (`ABC`) but fails in trials 2 and 3 (`AC`). This inconsistency at temperature 0 is notable — the model is not deterministic on this task despite zero temperature, suggesting it is genuinely uncertain.

**T19 — string equality through object:** GPT-OSS predicts `False` for the third comparison every trial. Unlike Sonnet and Haiku, it does not know that `"hello"` is an interned literal, so it treats all three object comparisons as reference-unequal.

**T31 — throwing when-filter:** GPT-OSS returns an empty response or partial output (`F` without `B`). It cannot trace the exception propagation when the `when` clause itself throws — a deeply unusual control flow that requires knowing the filter runs outside the catch frame.

**T35 — iterator finally:** GPT-OSS returns empty on all three trials. This is a complete failure to reason about iterator state machines and the Dispose contract.

**T42 — nullable null comparisons:** GPT-OSS returns empty on all three trials. The combination of null arithmetic (returns null) and null comparison (returns false, not null) is a lifted-operator subtlety that all three models miss entirely.

**T45 — delegate remove-last:** GPT-OSS returns empty on all three trials. The semantics of `Delegate.Remove` searching from the end are evidently absent from its training distribution.

---

## Key Finding

Four tasks were failed by every model on every trial: **T42** (nullable comparisons), **T45** (delegate remove-last), **T31** (throwing when-filter), and **T35** (iterator disposal). These are not obscure or theoretical — they appear in real production C# code. The consistent failures suggest these edge cases are simply underrepresented in training data across all three model families.

No model dominates. Sonnet passes T31 and T35; GPT-OSS fails both. GPT-OSS passes T02, T11, T23, and T28; Sonnet fails all four. This pattern suggests the models have each memorized different subsets of C# behavior rather than building a correct underlying runtime model.

## How to Reproduce

Requires .NET 10 SDK, an Anthropic API key, and a Groq API key (free at console.groq.com).

```powershell
$env:ANTHROPIC_API_KEY = "your-key"
$env:GROQ_API_KEY      = "your-key"
dotnet run --project src/DotNetSQLiEval.Cli -- v3 tasks/v3hard results/output
```

Results are saved as JSON in the output directory, one file per model per task per trial.
