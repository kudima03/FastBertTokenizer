// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Shouldly;

namespace FastBertTokenizer.Tests;

public class LoadTokenizer
{
    [Theory]
    [InlineData("data/baai-bge-small-en/tokenizer.json")]
    [InlineData("data/bert-base-chinese/tokenizer.json")]
    [InlineData("data/bert-base-multilingual-cased/tokenizer.json")]
    [InlineData("data/bert-base-uncased/tokenizer.json")]
    public async Task LoadTokenizerFromJsonAsync(string path)
    {
        var tokenizer = new BertTokenizer();
        await tokenizer.LoadTokenizerJsonAsync(path);
    }

    [Theory]
    [InlineData("data/baai-bge-small-en/tokenizer.json")]
    [InlineData("data/bert-base-chinese/tokenizer.json")]
    [InlineData("data/bert-base-multilingual-cased/tokenizer.json")]
    [InlineData("data/bert-base-uncased/tokenizer.json")]
    public void LoadTokenizerFromJsonSync(string path)
    {
        var stream = File.OpenRead(path);
        var tokenizer = new BertTokenizer();
        tokenizer.LoadTokenizerJson(stream);
    }

    [Theory]
    [InlineData("data/baai-bge-small-en/vocab.txt", true)]
    [InlineData("data/bert-base-chinese/vocab.txt", true)]
    [InlineData("data/bert-base-multilingual-cased/vocab.txt", true)]
    [InlineData("data/bert-base-uncased/vocab.txt", true)]
    public async Task LoadTokenizerFromVocabAsync(string path, bool lowercase)
    {
        var tokenizer = new BertTokenizer();
        await tokenizer.LoadVocabularyAsync(path, lowercase);
    }

    [Theory]
    [InlineData("data/baai-bge-small-en/vocab.txt", true)]
    [InlineData("data/bert-base-chinese/vocab.txt", true)]
    [InlineData("data/bert-base-multilingual-cased/vocab.txt", true)]
    [InlineData("data/bert-base-uncased/vocab.txt", true)]
    public void LoadTokenizerFromVocabSync(string path, bool lowercase)
    {
        var stream = File.OpenText(path);
        var tokenizer = new BertTokenizer();
        tokenizer.LoadVocabulary(stream, lowercase);
    }

    [Theory]
    [InlineData("data/invalid/wrong-version.json")]
    [InlineData("data/invalid/wrong-model-type.json")]
    [InlineData("data/invalid/wrong-normalizer.json")]
    [InlineData("data/invalid/wrong-pretokenizer.json")]
    [InlineData("data/invalid/dont-strip-accents.json")]
    [InlineData("data/invalid/dont-handle-chinese-chars.json")]
    [InlineData("data/invalid/dont-clean-text.json")]
    public async Task LoadTokenizerFromInvalidJsonAsync(string path)
    {
        var tokenizer = new BertTokenizer();
        await Should.ThrowAsync<ArgumentException>(tokenizer.LoadTokenizerJsonAsync(path));
    }

    [Theory]
    [InlineData("bert-base-uncased")]
    public async Task LoadFromHuggingFace(string huggingFaceRepo)
    {
        var tokenizer = new BertTokenizer();
        await tokenizer.LoadFromHuggingFaceAsync(huggingFaceRepo);
    }

    [Fact]
    public async Task PreventLoadAfterLoad()
    {
        var tokenizer = new BertTokenizer();
        await tokenizer.LoadTokenizerJsonAsync("data/bert-base-uncased/tokenizer.json");
        await Assert.ThrowsAsync<InvalidOperationException>(() => tokenizer.LoadTokenizerJsonAsync("data/bert-base-uncased/tokenizer.json"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => tokenizer.LoadVocabularyAsync("data/bert-base-uncased/vocab.txt", true));
    }
}
