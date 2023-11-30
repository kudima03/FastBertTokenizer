// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using RustLibWrapper;
using Shouldly;

namespace FastBertTokenizer.Tests;

[Collection("UsesRustLib")]
public class AsyncBatchEnumeratorVsHuggingface : IAsyncLifetime
{
    private BertTokenizer _baaiBgeTok = new();
    private BertTokenizer _bertUncasedTok = new();
    private BertTokenizer _bertMultilingualTok = new();
    private BertTokenizer _bertChineseTok = new();

    public async Task InitializeAsync()
    {
        await _baaiBgeTok.LoadVocabularyAsync("data/baai-bge-small-en/vocab.txt", true);
        await _bertUncasedTok.LoadVocabularyAsync("data/bert-base-uncased/vocab.txt", true);
        await _bertMultilingualTok.LoadVocabularyAsync("data/bert-base-multilingual-cased/vocab.txt", false);
        await _bertChineseTok.LoadVocabularyAsync("data/bert-base-chinese/vocab.txt", true);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [MemberData(nameof(WikipediaSimpleData.GetArticlesDict), MemberType = typeof(WikipediaSimpleData))]
    public async Task CompareSimpleWikipediaCorpusAsIsBertUncased512(Dictionary<int, string> articles)
    {
        RustTokenizer.LoadTokenizer("data/bert-base-uncased/tokenizer.json", 512);
        await CompareSimpleWikipediaCorpusAsIsImpl(_bertUncasedTok, articles, 512, 100);
    }

    [Theory(Skip = "Does not work yet")]
    [MemberData(nameof(WikipediaSimpleData.GetArticlesDict), MemberType = typeof(WikipediaSimpleData))]
    public async Task CompareSimpleWikipediaCorpusAsIsBertMultilingualBge512(Dictionary<int, string> articles)
    {
        RustTokenizer.LoadTokenizer("data/bert-base-multilingual-cased/tokenizer.json", 512);
        await CompareSimpleWikipediaCorpusAsIsImpl(_bertMultilingualTok, articles, 512, 100);
    }

    [Theory(Skip = "Does not work yet")]
    [MemberData(nameof(WikipediaSimpleData.GetArticlesDict), MemberType = typeof(WikipediaSimpleData))]
    public async Task CompareSimpleWikipediaCorpusAsIsBertChineseBge512(Dictionary<int, string> articles)
    {
        RustTokenizer.LoadTokenizer("data/bert-base-chinese/tokenizer.json", 512);
        await CompareSimpleWikipediaCorpusAsIsImpl(_bertChineseTok, articles, 512, 100);
    }

    [Theory]
    [MemberData(nameof(WikipediaSimpleData.GetArticlesDict), MemberType = typeof(WikipediaSimpleData))]
    public async Task CompareSimpleWikipediaCorpusAsIsBaaiBge512(Dictionary<int, string> articles)
    {
        RustTokenizer.LoadTokenizer("data/baai-bge-small-en/tokenizer.json", 512);
        await CompareSimpleWikipediaCorpusAsIsImpl(_baaiBgeTok, articles, 512, 100);
    }

    [Theory]
    [MemberData(nameof(WikipediaSimpleData.GetArticlesDict), MemberType = typeof(WikipediaSimpleData))]
    public async Task CompareSimpleWikipediaCorpusAsIsBaaiBge333(Dictionary<int, string> articles)
    {
        RustTokenizer.LoadTokenizer("data/baai-bge-small-en/tokenizer.json", 333);
        await CompareSimpleWikipediaCorpusAsIsImpl(_baaiBgeTok, articles, 333, 100);
    }

    [Theory]
    [MemberData(nameof(WikipediaSimpleData.GetArticlesDict), MemberType = typeof(WikipediaSimpleData))]
    public async Task CompareSimpleWikipediaCorpusAsIsBaaiBge27(Dictionary<int, string> articles)
    {
        RustTokenizer.LoadTokenizer("data/baai-bge-small-en/tokenizer.json", 27);
        await CompareSimpleWikipediaCorpusAsIsImpl(_baaiBgeTok, articles, 27, 100);
    }

    [Theory]
    [MemberData(nameof(WikipediaSimpleData.GetArticlesDict), MemberType = typeof(WikipediaSimpleData))]
    public async Task CompareSimpleWikipediaCorpusAsIsBaaiBge2048(Dictionary<int, string> articles)
    {
        RustTokenizer.LoadTokenizer("data/baai-bge-small-en/tokenizer.json", 2048);
        await CompareSimpleWikipediaCorpusAsIsImpl(_baaiBgeTok, articles, 2048, 100);
    }

    private async Task CompareSimpleWikipediaCorpusAsIsImpl(BertTokenizer uut, Dictionary<int, string> articles, int maxInputTokens, int batchSize)
    {
        async IAsyncEnumerable<(int, string)> EnumerateContent()
        {
            await Task.Delay(1);
            foreach (var (key, value) in articles)
            {
                yield return (key, value);
            }
        }

        var allNulls = new long[maxInputTokens];
        await foreach (var batch in uut.CreateAsyncBatchEnumerator(EnumerateContent(), maxInputTokens, batchSize, 0))
        {
            for (var i = 0; i < batch.InputIds.Length / maxInputTokens; i++)
            {
                var tokRgNullable = batch.OutputCorrelation.Span[i];
                if (!tokRgNullable.HasValue)
                {
                    batch.InputIds.Slice(i * maxInputTokens, maxInputTokens).ShouldBe(allNulls);
                    batch.AttentionMask.Slice(i * maxInputTokens, maxInputTokens).ShouldBe(allNulls);
                    continue;
                }

                var tokRg = tokRgNullable.Value;
                if (tokRg.Offset > 0)
                {
                    continue;
                }

                if (tokRg.Key == 6309 || tokRg.Key == 30153 || tokRg.Key == 60246)
                {
                    continue;
                }

                var content = articles[tokRg.Key];
                var huggF = RustTokenizer.TokenizeAndGetIds(content, maxInputTokens);

                var currentInputIds = batch.InputIds.Slice(i * maxInputTokens, maxInputTokens);
                try
                {
                    currentInputIds.ShouldBe(huggF.InputIds);
                    batch.AttentionMask.Slice(i * maxInputTokens, maxInputTokens).ShouldBe(huggF.AttentionMask);
                }
                catch (Exception exFirst)
                {
                    try
                    {
                        var needsToMatchUpToIdx = currentInputIds.Length - 1;

                        // assume [PAD] == 0 here
                        if (currentInputIds.Span[^1] == 0)
                        {
                            // Our result ends with padding. That might be because we don't tokenize partial words, while
                            // Hugging Face does.
                            while (currentInputIds.Span[--needsToMatchUpToIdx] == 0)
                            {
                                // Skip padding at the end if there is any. There might be because we didn't tokenize
                                // partial words.
                            }

                            // But then there neds to be a [SEP], otherwise there is some fault.
                            if (currentInputIds.Span[needsToMatchUpToIdx] != 102)
                            {
                                throw new Exception($"Error comparing tokenization results for {tokRg.Key}", exFirst);
                            }

                            // It was a [SEP], so we skip it.
                            needsToMatchUpToIdx--;
                        }
                        else if (currentInputIds.Span[^1] == 102) // assume [SEP] == 102 here
                        {
                            // Our result ends with [SEP]. As FastBertTokenizer searches for partial words first,
                            // while Huggingface removes diacritics first, the last token before [SEP] might be
                            // different. We accept that.
                            needsToMatchUpToIdx--; // skip [SEP]
                            needsToMatchUpToIdx--; // skip the token before [SEP]
                        }
                        else
                        {
                            throw new Exception($"Error comparing tokenization results for {tokRg.Key}", exFirst);
                        }

                        var needsToMatchUpToLen = needsToMatchUpToIdx + 1;
                        currentInputIds.Slice(0, needsToMatchUpToLen).ShouldBe(huggF.InputIds.Slice(0, needsToMatchUpToLen));
                        batch.AttentionMask.Slice(i * maxInputTokens, needsToMatchUpToLen).ShouldBe(huggF.AttentionMask.Slice(0, needsToMatchUpToLen));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error comparing tokenization results for {tokRg.Key}", ex);
                    }
                }
            }
        }
    }
}
