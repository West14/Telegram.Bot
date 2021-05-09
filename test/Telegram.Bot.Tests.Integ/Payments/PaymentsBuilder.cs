using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;

#nullable enable

namespace Telegram.Bot.Tests.Integ.Payments
{
    public class PaymentsBuilder
    {
        private readonly List<ShippingOption> _shippingOptions = new();
        private Product? _product;
        private string? _currency;
        private string? _startParameter;
        private string? _payload;
        private long? _chatId;
        private bool? _needName;
        private bool? _needEmail;
        private bool? _needShippingAddress;
        private bool? _needPhoneNumber;
        private bool? _isFlexible;
        private bool? _sendEmailToProvider;
        private bool? _sendPhoneNumberToProvider;
        private string? _providerData;
        private InlineKeyboardMarkup? _replyMarkup;
        private string? _paymentsProviderToken;

        public PaymentsBuilder WithCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException($"{nameof(currency)} is null or empty");
            _currency = currency;
            return this;
        }

        public PaymentsBuilder WithStartParameter(string startParameter)
        {
            if (string.IsNullOrWhiteSpace(startParameter)) throw new ArgumentException($"{nameof(startParameter)} is null or empty");
            _startParameter = startParameter;
            return this;
        }

        public PaymentsBuilder WithPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) throw new ArgumentException($"{nameof(payload)} is null or empty");
            _payload = payload;
            return this;
        }

        public PaymentsBuilder WithProviderData(string providerData)
        {
            if (string.IsNullOrWhiteSpace(providerData)) throw new ArgumentException($"{nameof(providerData)} is null or empty");
            _providerData = providerData;
            return this;
        }

        public PaymentsBuilder WithReplyMarkup(InlineKeyboardMarkup replyMarkup)
        {
            _replyMarkup = replyMarkup ?? throw new ArgumentNullException(nameof(replyMarkup));
            return this;
        }

        public PaymentsBuilder ToChat(long chatId)
        {
            _chatId = chatId;
            return this;
        }

        public PaymentsBuilder RequireName(bool require = true)
        {
            _needName = require;
            return this;
        }

        public PaymentsBuilder RequireEmail(bool require = true)
        {
            _needEmail = require;
            return this;
        }

        public PaymentsBuilder RequirePhoneNumber(bool require = true)
        {
            _needPhoneNumber = require;
            return this;
        }

        public PaymentsBuilder RequireShippingAddress(bool require = true)
        {
            _needShippingAddress = require;
            return this;
        }

        public PaymentsBuilder WithFlexible(bool value = true)
        {
            _isFlexible = value;
            return this;
        }

        public PaymentsBuilder SendEmailToProvider(bool send = true)
        {
            _sendEmailToProvider = send;
            return this;
        }

        public PaymentsBuilder SendPhoneNumberToProvider(bool send = true)
        {
            _sendPhoneNumberToProvider = send;
            return this;
        }

        public PaymentsBuilder WithPaymentProviderToken(string paymentsProviderToken)
        {
            if (string.IsNullOrWhiteSpace(paymentsProviderToken)) throw new ArgumentException($"{nameof(paymentsProviderToken)} is null or empty");
            _paymentsProviderToken = paymentsProviderToken;
            return this;
        }

        public PreliminaryInvoice GetPreliminaryInvoice()
        {
            if (_product is null) throw new InvalidOperationException("Product wasn't added");
            if (string.IsNullOrWhiteSpace(_currency)) throw new InvalidOperationException("Currency isn't set");

            return new()
            {
                Title = _product.Title,
                Currency = _currency,
                StartParameter = _startParameter,
                TotalAmount = _product.ProductPrices.Sum(price => price.Amount),
                Description = _product.Description,
            };
        }

        public Shipping GetShippingOptions() => new(_shippingOptions.ToArray());

        public int GetTotalAmount() =>
            (_product?.ProductPrices.Sum(price => price.Amount) ?? 0) +
            _shippingOptions.Sum(x => x.Prices.Sum(p => p.Amount));

        public int GetTotalAmountWithoutShippingCost() => _product?.ProductPrices.Sum(price => price.Amount) ?? 0;

        public SendInvoiceRequest BuildInvoiceRequest()
        {
            if (_product is null) throw new InvalidOperationException("Product wasn't added");
            if (string.IsNullOrWhiteSpace(_paymentsProviderToken)) throw new ArgumentException("Payments provider token is null or empty");
            if (_chatId is null) throw new InvalidOperationException("ChatId is null");
            if (string.IsNullOrWhiteSpace(_currency)) throw new InvalidOperationException("Currency isn't set");
            if (string.IsNullOrWhiteSpace(_payload)) throw new InvalidOperationException("Payload isn't set");

            return new(
                chatId: _chatId.Value,
                title: _product.Title,
                description: _product.Description,
                payload: _payload,
                providerToken: _paymentsProviderToken,
                currency: _currency,
                prices: _product.ProductPrices)
            {
                PhotoUrl = _product.PhotoUrl,
                PhotoWidth = _product.PhotoWidth,
                PhotoHeight = _product.PhotoHeight,
                NeedShippingAddress = _needShippingAddress,
                IsFlexible = _isFlexible,
                NeedName = _needName,
                NeedEmail = _needEmail,
                NeedPhoneNumber = _needPhoneNumber,
                SendEmailToProvider = _sendEmailToProvider,
                SendPhoneNumberToProvider = _sendPhoneNumberToProvider,
                StartParameter = _startParameter,
                ProviderData = _providerData,
                ReplyMarkup = _replyMarkup
            };
        }

        public AnswerShippingQueryRequest BuildShippingQueryRequest(string shippingQueryId, string? errorMessage = default)
        {
            if (string.IsNullOrWhiteSpace(shippingQueryId)) throw new ArgumentNullException(nameof(shippingQueryId));

            AnswerShippingQueryRequest shippingQueryRequest = errorMessage is null
                ? new(shippingQueryId, _shippingOptions)
                : new(shippingQueryId, errorMessage);

            return shippingQueryRequest;
        }

        public PaymentsBuilder WithProduct(Action<ProductBuilder> builder)
        {
            ProductBuilder productBuilder = new();
            builder(productBuilder);

            _product = productBuilder.Build();

            return this;
        }

        public PaymentsBuilder WithShipping(Action<ShippingOptionsBuilder> config)
        {
            ShippingOptionsBuilder builder = new();
            config(builder);

            _shippingOptions.Add(builder.Build());

            return this;
        }

        public class ShippingOptionsBuilder
        {
            private string? _id;
            private string? _title;
            private readonly List<LabeledPrice> _shippingPrices = new();

            public ShippingOptionsBuilder WithId(string id)
            {
                if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
                _id = id;
                return this;
            }

            public ShippingOptionsBuilder WithTitle(string title)
            {
                if (string.IsNullOrWhiteSpace(title)) throw new ArgumentNullException(nameof(title));
                _title = title;
                return this;
            }

            public ShippingOptionsBuilder WithPrice(string label, int amount)
            {
                if (string.IsNullOrWhiteSpace(label)) throw new ArgumentNullException(nameof(label));
                if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Price must be greater than 0");

                _shippingPrices.Add(new(label, amount));

                return this;
            }

            public ShippingOption Build() =>
                new()
                {
                    Id = _id ?? throw new InvalidOperationException("Id is null"),
                    Title = _title ?? throw new InvalidOperationException("Title is null"),
                    Prices = _shippingPrices.Any()
                        ? _shippingPrices.ToArray()
                        : throw new InvalidOperationException("Shipping prices are empty")
                };
        }

        public class ProductBuilder
        {
            private string? _description;
            private string? _title;
            private string? _photoUrl;
            private int _photoWidth;
            private int _photoHeight;
            private readonly List<LabeledPrice> _productPrices = new();

            public ProductBuilder WithTitle(string title)
            {
                if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException($"{nameof(title)} is null or empty");
                _title = title;
                return this;
            }

            public ProductBuilder WithDescription(string description)
            {
                if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException($"{nameof(description)} is null or empty");
                _description = description;
                return this;
            }

            public ProductBuilder WithPhoto(string url, int width, int height)
            {
                if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException($"{nameof(url)} is null or empty");
                if (width < 1 || height < 1) throw new ArgumentException("Dimensions are invalid");

                _photoUrl = url;
                _photoWidth = width;
                _photoHeight = height;

                return this;
            }

            public ProductBuilder WithProductPrice(string label, int amount)
            {
                if (string.IsNullOrWhiteSpace(label)) throw new ArgumentNullException(nameof(label));
                if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Price must be greater than 0");
                _productPrices.Add(new(label, amount));
                return this;
            }

            public Product Build() =>
                new()
                {
                    Title = _title ?? throw new InvalidOperationException("Title is null"),
                    Description = _description  ?? throw new InvalidOperationException("Description is null"),
                    PhotoUrl = _photoUrl,
                    PhotoHeight = _photoHeight,
                    PhotoWidth = _photoWidth,
                    ProductPrices = _productPrices.Any()
                        ? _productPrices.ToArray()
                        : throw new InvalidOperationException("Prices are empty")
                };
        }

        public record Product
        {
            public string Title { get; init; } = default!;
            public string Description { get; init; } = default!;
            public string? PhotoUrl { get; init; }
            public int PhotoWidth { get; init; }
            public int PhotoHeight { get; init; }
            public IReadOnlyCollection<LabeledPrice> ProductPrices { get; init; } = default!;
        }

        public record PreliminaryInvoice
        {
            public string Title { get; init; } = default!;
            public string Description { get; init; } = default!;
            public string? StartParameter { get; init; }
            public string Currency { get; init; } = default!;
            public int TotalAmount { get; init; }
        }

        public record Shipping(IReadOnlyList<ShippingOption> ShippingOptions)
        {
            public int TotalAmount => ShippingOptions.Sum(x => x.Prices.Sum(p => p.Amount));
        }
    }
}
