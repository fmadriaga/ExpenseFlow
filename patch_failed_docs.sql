BEGIN TRANSACTION;

-- Doc 65: 'PIGALLE' | 2197.0 RON | 3 items → Success
UPDATE Documents SET
  MerchantName = 'PIGALLE',
  TransactionDate = NULL,
  TotalAmount = 2197.0,
  TaxAmount = NULL,
  Currency = 'RON',
  OcrStatus = 'Success',
  Confidence = 66.67,
  ErrorMessage = NULL
WHERE Id = 65;

DELETE FROM DocumentLines WHERE DocumentId = 65;
INSERT INTO DocumentLines (DocumentId, Description, Quantity, UnitPrice, Amount, Currency) VALUES (65, NULL, 100.0, 3700.0, 821.0, 'RON');
INSERT INTO DocumentLines (DocumentId, Description, Quantity, UnitPrice, Amount, Currency) VALUES (65, NULL, 0.0, 186.0, 168.0, 'RON');
INSERT INTO DocumentLines (DocumentId, Description, Quantity, UnitPrice, Amount, Currency) VALUES (65, 'Mousse por reBondes', 0.0, 0.0, 0.0, 'RON');

-- Doc 66: 'farmacias\nPIGALLE' | 2597.0 UYU | 5 items → Success
UPDATE Documents SET
  MerchantName = 'farmacias
PIGALLE',
  TransactionDate = '2026-04-10',
  TotalAmount = 2597.0,
  TaxAmount = NULL,
  Currency = 'UYU',
  OcrStatus = 'Success',
  Confidence = 83.33,
  ErrorMessage = NULL
WHERE Id = 66;

DELETE FROM DocumentLines WHERE DocumentId = 66;
INSERT INTO DocumentLines (DocumentId, Description, Quantity, UnitPrice, Amount, Currency) VALUES (66, 'BABYSEC PREMIUM SUPER JUMBO
PACK XXG [96 uni.]', 1.0, 1711.2, 1711.2, 'UYU');
INSERT INTO DocumentLines (DocumentId, Description, Quantity, UnitPrice, Amount, Currency) VALUES (66, 'BABYSEC TOALLITAS ULTRA PACK
[250 uni.]', 1.0, 321.6, 321.6, 'UYU');
INSERT INTO DocumentLines (DocumentId, Description, Quantity, UnitPrice, Amount, Currency) VALUES (66, 'ANN BOW JABON GLICERINA
CUADRADO PACK × 3 [360 gr]', 3.0, 188.0, 564.0, 'UYU');
INSERT INTO DocumentLines (DocumentId, Description, Quantity, UnitPrice, Amount, Currency) VALUES (66, 'BOLSA TNT CHICA [1 uni.]', 1.0, 0.0, 0.0, 'UYU');
INSERT INTO DocumentLines (DocumentId, Description, Quantity, UnitPrice, Amount, Currency) VALUES (66, 'Ajuste por redondeo', NULL, NULL, 0.0, 'UYU');

COMMIT;
