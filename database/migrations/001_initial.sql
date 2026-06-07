create extension if not exists pgcrypto;
create extension if not exists pg_trgm;
create extension if not exists unaccent;
create extension if not exists pg_stat_statements;

create schema if not exists administration;
create schema if not exists identity;
create schema if not exists audit;
create schema if not exists catalog;
create schema if not exists purchasing;
create schema if not exists inventory;
create schema if not exists sales;
create schema if not exists fiscal;
create schema if not exists finance;
create schema if not exists crm;
create schema if not exists integrations;

create or replace function administration.current_company_id()
returns uuid
language sql
stable
as $$ select nullif(current_setting('atlas.company_id', true), '')::uuid $$;

create or replace function administration.current_branch_id()
returns uuid
language sql
stable
as $$ select nullif(current_setting('atlas.branch_id', true), '')::uuid $$;

create table if not exists administration.companies (
  "Id" uuid primary key default gen_random_uuid(),
  "LegalName" varchar(220) not null,
  "TradeName" varchar(180) not null,
  "Cnpj" varchar(32) not null unique,
  "StateRegistration" varchar(32),
  "FiscalState" varchar(2) not null,
  "TaxRegime" varchar(40) not null
);

create table if not exists administration.branches (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null references administration.companies("Id"),
  "BranchId" uuid not null,
  "Name" varchar(180) not null,
  "Cnpj" varchar(32) not null,
  "StateRegistration" varchar(32),
  "FiscalState" varchar(2) not null,
  "WarehouseCode" varchar(40) not null,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0,
  unique ("CompanyId", "Cnpj")
);

create table if not exists identity.module_permissions (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "RoleName" varchar(120) not null,
  "Module" varchar(80) not null,
  "Permission" varchar(120) not null,
  "RequiresMfa" boolean not null default false,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0,
  unique ("CompanyId", "BranchId", "RoleName", "Module", "Permission")
);

create table if not exists identity.user_profiles (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "SubjectId" varchar(160) not null,
  "DisplayName" varchar(180) not null,
  "Email" varchar(180) not null,
  "SensitiveProfile" boolean not null default false,
  "Active" boolean not null default true,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0,
  unique ("CompanyId", "SubjectId")
);

create table if not exists identity.local_users (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "Username" varchar(120) not null,
  "DisplayName" varchar(180) not null,
  "Email" varchar(180) not null,
  "PasswordHash" varchar(400) not null,
  "RolesJson" jsonb not null default '[]',
  "Active" boolean not null default true,
  "SensitiveProfile" boolean not null default false,
  "MfaEnabled" boolean not null default false,
  "FailedAccessCount" integer not null default 0,
  "LockedUntil" timestamptz,
  "PasswordChangedAt" timestamptz not null default now(),
  "LastLoginAt" timestamptz,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0,
  unique ("CompanyId", "Username")
);

create table if not exists identity.local_refresh_tokens (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "UserId" uuid not null references identity.local_users("Id") on delete cascade,
  "TokenHash" varchar(80) not null unique,
  "ExpiresAt" timestamptz not null,
  "RevokedAt" timestamptz,
  "CreatedByIp" varchar(80),
  "RevokedByIp" varchar(80),
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists identity.authentication_logs (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "Username" varchar(120) not null,
  "UserId" uuid,
  "Success" boolean not null,
  "Reason" varchar(120) not null,
  "IpAddress" varchar(80),
  "OccurredAt" timestamptz not null,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists audit.audit_records (
  "Id" uuid not null default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "EntityName" varchar(180) not null,
  "EntityId" varchar(120) not null,
  "Operation" varchar(40) not null,
  "Before" jsonb not null default '{}',
  "After" jsonb not null default '{}',
  "UserId" varchar(160) not null,
  "IpAddress" varchar(80),
  "OccurredAt" timestamptz not null default now(),
  primary key ("Id", "OccurredAt")
) partition by range ("OccurredAt");

create table if not exists audit.audit_records_2026 partition of audit.audit_records
for values from ('2026-01-01') to ('2027-01-01');

create table if not exists catalog.products (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "Sku" varchar(80) not null,
  "Description" varchar(420) not null,
  "Gtin" varchar(32) not null default '',
  "Ncm" varchar(16) not null,
  "Cest" varchar(16),
  "CommercialUnit" varchar(8) not null default 'UN',
  "TaxUnit" varchar(8) not null default 'UN',
  "BrandName" varchar(120) not null,
  "ProductLine" varchar(120) not null,
  "ManufacturerName" varchar(160) not null,
  "ManufacturerCode" varchar(120) not null,
  "OeCode" varchar(120) not null default '',
  "Origin" varchar(4) not null default '0',
  "WeightKg" numeric(18,4) not null default 0,
  "HeightCm" numeric(18,4),
  "WidthCm" numeric(18,4),
  "LengthCm" numeric(18,4),
  "TechnicalAttributes" jsonb not null default '{}',
  "PhotoUrls" jsonb not null default '[]',
  "IsKit" boolean not null default false,
  "KitComposition" jsonb not null default '[]',
  "LastCost" numeric(18,4) not null default 0,
  "MarginPercent" numeric(9,4) not null default 0,
  "SuggestedPrice" numeric(18,4) not null default 0,
  "DiscontinuedAt" timestamptz,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0,
  unique ("CompanyId", "Sku")
);

create index if not exists ix_products_code_search on catalog.products using gin (
  (
    coalesce("Sku",'') || ' ' || coalesce("Description",'') || ' ' || coalesce("Gtin",'') || ' ' ||
    coalesce("ManufacturerCode",'') || ' ' || coalesce("OeCode",'')
  ) gin_trgm_ops
);
create index if not exists ix_products_gtin on catalog.products ("CompanyId", "Gtin");
create index if not exists ix_products_ncm_cest on catalog.products ("CompanyId", "Ncm", "Cest");

create table if not exists catalog.product_applications (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "ProductId" uuid not null references catalog.products("Id") on delete cascade,
  "Make" varchar(120) not null,
  "Model" varchar(160) not null,
  "FromYear" integer not null,
  "ToYear" integer,
  "Engine" varchar(120) not null,
  "Fuel" varchar(40) not null,
  "ChassisFrom" varchar(40),
  "ChassisTo" varchar(40),
  "VinFilterExpression" varchar(600),
  "Notes" text,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists catalog.product_equivalents (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "ProductId" uuid not null references catalog.products("Id") on delete cascade,
  "Code" varchar(120) not null,
  "Type" varchar(40) not null,
  "Brand" varchar(120),
  "Notes" text,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists catalog.product_supplier_offers (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "ProductId" uuid not null references catalog.products("Id") on delete cascade,
  "SupplierId" uuid not null,
  "SupplierCode" varchar(120) not null,
  "LastCost" numeric(18,4) not null,
  "LeadTimeDays" integer not null default 0,
  "Preferred" boolean not null default false,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists catalog.product_price_history (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "ProductId" uuid not null references catalog.products("Id") on delete cascade,
  "PriceTable" varchar(80) not null,
  "Cost" numeric(18,4) not null,
  "MarginPercent" numeric(9,4) not null,
  "Price" numeric(18,4) not null,
  "ValidFrom" date not null,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists catalog.vehicle_models (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "Make" varchar(120) not null,
  "Model" varchar(160) not null,
  "FromYear" integer not null,
  "ToYear" integer,
  "Engine" varchar(120) not null,
  "Fuel" varchar(40) not null,
  "ExternalCatalogRef" varchar(160) not null default '',
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists purchasing.suppliers (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "LegalName" varchar(220) not null,
  "TradeName" varchar(180) not null,
  "Document" varchar(32) not null,
  "StateRegistration" varchar(32),
  "Email" varchar(180),
  "Phone" varchar(40),
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0,
  unique ("CompanyId", "Document")
);

create table if not exists purchasing.purchase_orders (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "SupplierId" uuid not null,
  "SupplierNameSnapshot" varchar(180) not null,
  "Status" varchar(40) not null,
  "ExpectedOn" date not null,
  "ExternalReference" varchar(120) not null default '',
  "Total" numeric(18,4) not null default 0,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists purchasing.purchase_order_lines (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "PurchaseOrderId" uuid not null references purchasing.purchase_orders("Id") on delete cascade,
  "ProductId" uuid not null,
  "SupplierCode" varchar(120) not null,
  "Quantity" numeric(18,4) not null,
  "UnitCost" numeric(18,4) not null,
  "NcmSnapshot" varchar(16) not null,
  "CfopSnapshot" varchar(8) not null,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists inventory.stock_locations (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "Code" varchar(60) not null,
  "Name" varchar(160) not null,
  "Type" varchar(40) not null,
  "AllowsPicking" boolean not null default true,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0,
  unique ("CompanyId", "BranchId", "Code")
);

create table if not exists inventory.stock_balances (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "ProductId" uuid not null references catalog.products("Id"),
  "LocationId" uuid not null references inventory.stock_locations("Id"),
  "OnHandQuantity" numeric(18,4) not null default 0,
  "ReservedQuantity" numeric(18,4) not null default 0,
  "AvailableQuantity" numeric(18,4) not null default 0,
  "MinimumQuantity" numeric(18,4) not null default 0,
  "MaximumQuantity" numeric(18,4) not null default 0,
  "LastCost" numeric(18,4) not null default 0,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0,
  unique ("CompanyId", "BranchId", "ProductId", "LocationId")
);

create table if not exists inventory.stock_movements (
  "Id" uuid not null default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "ProductId" uuid not null,
  "LocationId" uuid not null,
  "Type" varchar(40) not null,
  "Quantity" numeric(18,4) not null,
  "UnitCost" numeric(18,4) not null default 0,
  "Reference" varchar(160) not null default '',
  "Reason" varchar(300) not null default '',
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0,
  primary key ("Id", "CreatedAt")
) partition by range ("CreatedAt");

create table if not exists inventory.stock_movements_2026 partition of inventory.stock_movements
for values from ('2026-01-01') to ('2027-01-01');

create table if not exists inventory.stock_reservations (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "ProductId" uuid not null,
  "LocationId" uuid not null,
  "Quantity" numeric(18,4) not null,
  "SourceDocumentId" uuid not null,
  "SourceType" varchar(60) not null,
  "ExpiresAt" timestamptz,
  "Status" varchar(40) not null,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists inventory.pick_lists (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "SourceDocumentId" uuid not null,
  "SourceType" varchar(60) not null,
  "Status" varchar(40) not null,
  "LinesJson" jsonb not null default '[]',
  "ConfirmedAt" timestamptz,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists crm.customers (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "Type" varchar(40) not null,
  "Name" varchar(220) not null,
  "Document" varchar(32) not null,
  "StateRegistration" varchar(32),
  "Email" varchar(180) not null default '',
  "Phone" varchar(40) not null default '',
  "PrimaryPlate" varchar(16) not null default '',
  "PrimaryChassis" varchar(40) not null default '',
  "Segment" varchar(80) not null default '',
  "InteractionsJson" jsonb not null default '[]',
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists sales.sales_quotes (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "CustomerId" uuid,
  "CustomerNameSnapshot" varchar(220) not null,
  "Status" varchar(40) not null,
  "ValidUntil" date not null,
  "Total" numeric(18,4) not null,
  "LinesJson" jsonb not null default '[]',
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists sales.sales_orders (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "QuoteId" uuid,
  "CustomerId" uuid,
  "CustomerNameSnapshot" varchar(220) not null,
  "Status" varchar(40) not null,
  "OperationNature" varchar(220) not null,
  "PriceTable" varchar(80) not null,
  "PaymentTerms" varchar(120) not null,
  "SalespersonId" varchar(160),
  "CommissionPercent" numeric(9,4) not null default 0,
  "Subtotal" numeric(18,4) not null default 0,
  "Discount" numeric(18,4) not null default 0,
  "Total" numeric(18,4) not null default 0,
  "RomaneioCode" varchar(80) not null default '',
  "ReturnReason" varchar(400),
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists sales.sales_order_lines (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "SalesOrderId" uuid not null references sales.sales_orders("Id") on delete cascade,
  "ProductId" uuid not null,
  "LocationId" uuid not null,
  "SkuSnapshot" varchar(80) not null,
  "DescriptionSnapshot" varchar(420) not null,
  "NcmSnapshot" varchar(16) not null,
  "CestSnapshot" varchar(16),
  "Quantity" numeric(18,4) not null,
  "UnitPrice" numeric(18,4) not null,
  "Discount" numeric(18,4) not null default 0,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists sales.cash_sessions (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "OperatorId" varchar(160) not null,
  "OpenedAt" timestamptz not null,
  "ClosedAt" timestamptz,
  "OpeningAmount" numeric(18,4) not null,
  "DeclaredAmount" numeric(18,4) not null default 0,
  "DifferenceAmount" numeric(18,4) not null default 0,
  "Status" varchar(40) not null,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists fiscal.fiscal_rules (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "SourceUf" varchar(2) not null,
  "DestinationUf" varchar(2) not null,
  "Regime" varchar(60) not null,
  "OperationType" varchar(80) not null,
  "Ncm" varchar(16) not null,
  "Cest" varchar(16),
  "Cfop" varchar(8) not null,
  "Cst" varchar(8),
  "Csosn" varchar(8),
  "Origin" varchar(4) not null,
  "IcmsRate" numeric(9,4) not null default 0,
  "IcmsStRate" numeric(9,4) not null default 0,
  "PisRate" numeric(9,4) not null default 0,
  "CofinsRate" numeric(9,4) not null default 0,
  "IbsRate" numeric(9,4) not null default 0,
  "CbsRate" numeric(9,4) not null default 0,
  "BenefitCode" varchar(60),
  "ExceptionCode" varchar(80),
  "LegalBasis" varchar(800) not null,
  "ValidFrom" date not null,
  "ValidTo" date,
  "Active" boolean not null default true,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists fiscal.fiscal_documents (
  "Id" uuid not null default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "SalesOrderId" uuid,
  "Model" varchar(8) not null,
  "Kind" varchar(40) not null,
  "Status" varchar(60) not null,
  "Provider" varchar(80) not null,
  "Environment" varchar(40) not null,
  "AccessKey" varchar(80) not null default '',
  "Number" varchar(20) not null,
  "Series" varchar(8) not null,
  "IssuedAt" timestamptz not null,
  "AuthorizationProtocol" varchar(80),
  "XmlHash" varchar(80) not null,
  "XmlStorageUri" varchar(600) not null,
  "PayloadSnapshot" jsonb not null default '{}',
  "TaxSnapshot" jsonb not null default '{}',
  "Contingency" boolean not null default false,
  "ContingencyReason" varchar(500),
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0,
  primary key ("Id", "IssuedAt")
) partition by range ("IssuedAt");

create table if not exists fiscal.fiscal_documents_2026 partition of fiscal.fiscal_documents
for values from ('2026-01-01') to ('2027-01-01');

create table if not exists fiscal.fiscal_document_items (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "FiscalDocumentId" uuid not null,
  "ProductId" uuid not null,
  "SkuSnapshot" varchar(80) not null,
  "NcmSnapshot" varchar(16) not null,
  "CestSnapshot" varchar(16),
  "Quantity" numeric(18,4) not null,
  "UnitAmount" numeric(18,4) not null,
  "TotalAmount" numeric(18,4) not null,
  "Cfop" varchar(8) not null,
  "Cst" varchar(8),
  "Csosn" varchar(8),
  "TaxesJson" jsonb not null default '{}',
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists fiscal.fiscal_document_events (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "FiscalDocumentId" uuid not null,
  "EventType" varchar(80) not null,
  "Status" varchar(80) not null,
  "Protocol" varchar(80),
  "Message" varchar(800),
  "RawPayload" jsonb not null default '{}',
  "OccurredAt" timestamptz not null,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists fiscal.dfe_distribution_cursors (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "Cnpj" varchar(32) not null,
  "Environment" varchar(40) not null,
  "LastNsu" bigint not null default 0,
  "MaxNsu" bigint not null default 0,
  "LastRunAt" timestamptz,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0,
  unique ("CompanyId", "BranchId", "Cnpj", "Environment")
);

create table if not exists finance.accounts_payable (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "SupplierId" uuid,
  "SupplierSnapshot" varchar(220) not null,
  "DocumentNumber" varchar(80) not null,
  "DueOn" date not null,
  "Amount" numeric(18,4) not null,
  "Status" varchar(40) not null,
  "SettledAt" timestamptz,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists finance.accounts_receivable (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "CustomerId" uuid,
  "CustomerSnapshot" varchar(220) not null,
  "DocumentNumber" varchar(80) not null,
  "DueOn" date not null,
  "Amount" numeric(18,4) not null,
  "Status" varchar(40) not null,
  "SettledAt" timestamptz,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists finance.cash_ledger_entries (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "AccountCode" varchar(40) not null,
  "Type" varchar(40) not null,
  "PostedOn" date not null,
  "Amount" numeric(18,4) not null,
  "Description" varchar(300) not null,
  "SourceDocumentId" uuid,
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0
);

create table if not exists integrations.integration_adapters (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "Type" varchar(80) not null,
  "Name" varchar(120) not null,
  "Enabled" boolean not null default false,
  "ConfigurationJson" jsonb not null default '{}',
  "CreatedAt" timestamptz not null default now(),
  "CreatedBy" varchar(160) not null default current_setting('atlas.user_id', true),
  "UpdatedAt" timestamptz,
  "UpdatedBy" varchar(160),
  "RowVersion" bigint not null default 0,
  unique ("CompanyId", "Type", "Name")
);

create table if not exists integrations.outbox_messages (
  "Id" uuid primary key default gen_random_uuid(),
  "CompanyId" uuid not null,
  "BranchId" uuid not null,
  "EventType" varchar(300) not null,
  "Payload" jsonb not null,
  "Headers" jsonb not null,
  "OccurredAt" timestamptz not null,
  "ProcessedAt" timestamptz,
  "FailedAt" timestamptz,
  "FailureReason" text,
  "Attempts" integer not null default 0
);

create index if not exists ix_product_applications_fitment on catalog.product_applications ("CompanyId", "Make", "Model", "FromYear", "ToYear");
create index if not exists ix_product_equivalents_code on catalog.product_equivalents ("CompanyId", "Code");
create index if not exists ix_stock_balance_lookup on inventory.stock_balances ("CompanyId", "BranchId", "ProductId", "LocationId");
create index if not exists ix_sales_orders_status on sales.sales_orders ("CompanyId", "BranchId", "Status", "CreatedAt");
create index if not exists ix_fiscal_rules_lookup on fiscal.fiscal_rules ("CompanyId", "SourceUf", "DestinationUf", "Regime", "OperationType", "Ncm", "Cest", "ValidFrom");
create index if not exists ix_fiscal_documents_access_key on fiscal.fiscal_documents ("CompanyId", "AccessKey");
create index if not exists ix_outbox_pending on integrations.outbox_messages ("ProcessedAt", "OccurredAt");

alter table administration.branches enable row level security;
alter table identity.module_permissions enable row level security;
alter table identity.user_profiles enable row level security;
alter table identity.local_users enable row level security;
alter table identity.local_refresh_tokens enable row level security;
alter table identity.authentication_logs enable row level security;
alter table audit.audit_records enable row level security;
alter table catalog.products enable row level security;
alter table catalog.product_applications enable row level security;
alter table catalog.product_equivalents enable row level security;
alter table catalog.product_supplier_offers enable row level security;
alter table catalog.product_price_history enable row level security;
alter table catalog.vehicle_models enable row level security;
alter table purchasing.suppliers enable row level security;
alter table purchasing.purchase_orders enable row level security;
alter table purchasing.purchase_order_lines enable row level security;
alter table inventory.stock_locations enable row level security;
alter table inventory.stock_balances enable row level security;
alter table inventory.stock_movements enable row level security;
alter table inventory.stock_reservations enable row level security;
alter table inventory.pick_lists enable row level security;
alter table crm.customers enable row level security;
alter table sales.sales_quotes enable row level security;
alter table sales.sales_orders enable row level security;
alter table sales.sales_order_lines enable row level security;
alter table sales.cash_sessions enable row level security;
alter table fiscal.fiscal_rules enable row level security;
alter table fiscal.fiscal_documents enable row level security;
alter table fiscal.fiscal_document_items enable row level security;
alter table fiscal.fiscal_document_events enable row level security;
alter table fiscal.dfe_distribution_cursors enable row level security;
alter table finance.accounts_payable enable row level security;
alter table finance.accounts_receivable enable row level security;
alter table finance.cash_ledger_entries enable row level security;
alter table integrations.integration_adapters enable row level security;
alter table integrations.outbox_messages enable row level security;

do $$
declare
  item record;
begin
  for item in
    select schemaname, tablename
    from pg_tables
    where schemaname in ('administration','identity','audit','catalog','purchasing','inventory','crm','sales','fiscal','finance','integrations')
      and tablename <> 'companies'
  loop
    execute format('drop policy if exists tenant_isolation on %I.%I', item.schemaname, item.tablename);
    execute format(
      'create policy tenant_isolation on %I.%I using ("CompanyId" = administration.current_company_id()) with check ("CompanyId" = administration.current_company_id())',
      item.schemaname,
      item.tablename
    );
  end loop;
end $$;
