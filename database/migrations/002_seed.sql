insert into administration.companies ("Id", "LegalName", "TradeName", "Cnpj", "StateRegistration", "FiscalState", "TaxRegime")
values (
  '11111111-1111-1111-1111-111111111111',
  'AutoParts ERP Homologacao Ltda',
  'AutoParts Pecas',
  '12345678000195',
  '110042490114',
  'SP',
  'simple_national'
) on conflict ("Id") do nothing;

insert into administration.branches ("Id", "CompanyId", "BranchId", "Name", "Cnpj", "StateRegistration", "FiscalState", "WarehouseCode", "CreatedAt", "CreatedBy")
values (
  '22222222-2222-2222-2222-222222222222',
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  'Matriz Sao Paulo',
  '12345678000195',
  '110042490114',
  'SP',
  'CD-SP',
  now(),
  'seed'
) on conflict ("Id") do nothing;

insert into identity.module_permissions ("Id", "CompanyId", "BranchId", "RoleName", "Module", "Permission", "RequiresMfa", "CreatedAt", "CreatedBy")
values
  (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 'admin', 'all', 'manage', true, now(), 'seed'),
  (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 'fiscal-manager', 'fiscal', 'issue', true, now(), 'seed'),
  (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 'counter-sales', 'sales', 'sell', false, now(), 'seed')
on conflict do nothing;

insert into identity.local_users (
  "Id", "CompanyId", "BranchId", "Username", "DisplayName", "Email", "PasswordHash", "RolesJson",
  "Active", "SensitiveProfile", "MfaEnabled", "CreatedAt", "CreatedBy"
)
values (
  '66666666-6666-6666-6666-666666666666',
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  'admin',
  'Administrador Local',
  'admin@autopartserp.local',
  'pbkdf2-sha256$210000$cJufcckL+JDfZdGFUKhcSA==$boBKsrsWDg3J/x/qXsme0T8gJH4J+Gs1M+Wk6hp5Q8c=',
  '["admin","manager","fiscal-manager","finance-manager"]',
  true,
  true,
  false,
  now(),
  'seed'
) on conflict ("CompanyId", "Username") do nothing;

insert into catalog.products (
  "Id", "CompanyId", "BranchId", "Sku", "Description", "Gtin", "Ncm", "Cest", "CommercialUnit", "TaxUnit",
  "BrandName", "ProductLine", "ManufacturerName", "ManufacturerCode", "OeCode", "Origin", "WeightKg",
  "TechnicalAttributes", "PhotoUrls", "LastCost", "MarginPercent", "SuggestedPrice", "CreatedAt", "CreatedBy"
)
values
  ('33333333-3333-3333-3333-333333333331','11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','NK-PAST-01234','Pastilha de freio dianteira ceramica Corolla 2.0 2015-2019','7890000001234','87083019','0105700','UN','UN','Nakata','Freios','Nakata Automotiva','NKF1234','04465-0K290','0',1.2500,'{"material":"ceramica","eixo":"dianteiro","sensor_desgaste":false}','[]',96.5000,48.0000,142.8200,now(),'seed'),
  ('33333333-3333-3333-3333-333333333332','11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','FR-DISCO-9876','Disco de freio ventilado HR/V HR 2.5 Diesel 2013-2021','7890000009876','87083019','0105700','UN','UN','Fremax','Freios','Fremax','BD9876','51712-4H000','0',7.9000,'{"diametro_mm":300,"espessura_mm":28,"furos":5}','[]',214.0000,42.0000,303.8800,now(),'seed'),
  ('33333333-3333-3333-3333-333333333333','11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','MAH-FILTRO-LX123','Filtro de ar motor Sprinter 415 CDI 2.2 Diesel 2012-2024','7890000004567','84213100',null,'UN','UN','Mahle','Filtros','Mahle Metal Leve','LX123','A6510940004','0',0.8400,'{"comprimento_mm":310,"largura_mm":185,"altura_mm":52}','[]',58.2000,55.0000,90.2100,now(),'seed')
on conflict ("Id") do nothing;

insert into catalog.product_applications ("Id", "CompanyId", "BranchId", "ProductId", "Make", "Model", "FromYear", "ToYear", "Engine", "Fuel", "ChassisFrom", "ChassisTo", "VinFilterExpression", "Notes", "CreatedAt", "CreatedBy")
values
  (gen_random_uuid(),'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333331','Toyota','Corolla',2015,2019,'2.0 16V','flex',null,null,null,'Conferir sistema de freio antes da venda.',now(),'seed'),
  (gen_random_uuid(),'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333332','Hyundai','HR',2013,2021,'2.5 CRDi','diesel',null,null,null,'Aplicacao utilitario/caminhao leve.',now(),'seed'),
  (gen_random_uuid(),'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333333','Mercedes-Benz','Sprinter 415 CDI',2012,2024,'2.2 CDI','diesel','8AC90600000000000','8AC90699999999999','VIN startsWith 8AC906','Filtro de ar principal.',now(),'seed')
on conflict do nothing;

insert into catalog.product_equivalents ("Id", "CompanyId", "BranchId", "ProductId", "Code", "Type", "Brand", "Notes", "CreatedAt", "CreatedBy")
values
  (gen_random_uuid(),'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333331','04465-0K290','OE','Toyota','Codigo OE principal',now(),'seed'),
  (gen_random_uuid(),'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333331','PD/1470','EQUIVALENT','Fras-le','Equivalente aftermarket',now(),'seed'),
  (gen_random_uuid(),'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333333','A6510940004','OE','Mercedes-Benz','Codigo OE filtro Sprinter',now(),'seed')
on conflict do nothing;

insert into purchasing.suppliers ("Id", "CompanyId", "BranchId", "LegalName", "TradeName", "Document", "StateRegistration", "Email", "Phone", "CreatedAt", "CreatedBy")
values ('44444444-4444-4444-4444-444444444444','11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','Distribuidora Pecas Sul Ltda','Pecas Sul','98765432000110','110000000000','compras@pecassul.example','1133334444',now(),'seed')
on conflict ("Id") do nothing;

insert into inventory.stock_locations ("Id", "CompanyId", "BranchId", "Code", "Name", "Type", "AllowsPicking", "CreatedAt", "CreatedBy")
values
  ('55555555-5555-5555-5555-555555555551','11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','A1-01','Rua A Porta 1','warehouse',true,now(),'seed'),
  ('55555555-5555-5555-5555-555555555552','11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','BALCAO','Pronta entrega balcao','counter',true,now(),'seed')
on conflict ("Id") do nothing;

insert into inventory.stock_balances ("Id", "CompanyId", "BranchId", "ProductId", "LocationId", "OnHandQuantity", "ReservedQuantity", "AvailableQuantity", "MinimumQuantity", "MaximumQuantity", "LastCost", "CreatedAt", "CreatedBy")
values
  (gen_random_uuid(),'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333331','55555555-5555-5555-5555-555555555551',18,0,18,8,40,96.5,now(),'seed'),
  (gen_random_uuid(),'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333332','55555555-5555-5555-5555-555555555551',6,0,6,4,18,214.0,now(),'seed'),
  (gen_random_uuid(),'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333333','55555555-5555-5555-5555-555555555551',12,0,12,6,30,58.2,now(),'seed')
on conflict do nothing;

insert into fiscal.fiscal_rules (
  "Id", "CompanyId", "BranchId", "SourceUf", "DestinationUf", "Regime", "OperationType", "Ncm", "Cest", "Cfop", "Cst", "Csosn", "Origin",
  "IcmsRate", "IcmsStRate", "PisRate", "CofinsRate", "IbsRate", "CbsRate", "BenefitCode", "ExceptionCode", "LegalBasis", "ValidFrom", "ValidTo", "Active", "CreatedAt", "CreatedBy"
)
values
  (gen_random_uuid(),'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','SP','SP','simple_national','sale_goods','87083019','0105700','5102',null,'102','0',0,0,0,0,0,0,null,null,'Regra parametrizada de homologacao; substituir por tabela fiscal validada por contador.', '2026-01-01', null, true, now(),'seed'),
  (gen_random_uuid(),'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','SP','SP','simple_national','sale_goods','84213100',null,'5102',null,'102','0',0,0,0,0,0,0,null,null,'Regra parametrizada de homologacao para filtros.', '2026-01-01', null, true, now(),'seed')
on conflict do nothing;

insert into integrations.integration_adapters ("Id", "CompanyId", "BranchId", "Type", "Name", "Enabled", "ConfigurationJson", "CreatedAt", "CreatedBy")
values
  (gen_random_uuid(),'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','aftermarket_catalog','null',true,'{}',now(),'seed'),
  (gen_random_uuid(),'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','fiscal_provider','sandbox',true,'{"environment":"homologation"}',now(),'seed')
on conflict do nothing;
