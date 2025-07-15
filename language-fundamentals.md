# Cadenza Language Fundamentals

## Table of Contents
1. [Primitive Types](#primitive-types)
2. [Module System](#module-system)
3. [Functions](#functions)
4. [Effect System](#effect-system)
5. [Error Handling](#error-handling)
6. [Inter-Module Communication](#inter-module-communication)
7. [External Dependencies](#external-dependencies)
8. [API Endpoints](#api-endpoints)
9. [Data Structures](#data-structures)
10. [Control Flow](#control-flow)

## Primitive Types

### Basic Types
```cadenza
// Numeric types
Int8, Int16, Int32, Int64
UInt8, UInt16, UInt32, UInt64
Float32, Float64
Decimal  // For financial calculations

// Text types
String
Char
Bytes

// Boolean
Bool  // true, false

// Special types
Unit      // Equivalent to void, but explicit
Never     // For functions that never return
```

### Container Types
```cadenza
// Collections
List<T>          // Immutable list
Array<T>         // Mutable array  
Set<T>           // Immutable set
Map<K, V>        // Immutable map
Queue<T>         // FIFO queue
Stack<T>         // LIFO stack

// Optional types
Option<T>        // Some(value) or None
Result<T, E>     // Ok(value) or Error(error)
```

### Domain-Specific Types
```cadenza
// Built-in domain types
Email            // Validated email address
Url              // Validated URL
UUID             // Universally unique identifier
DateTime         // ISO 8601 datetime
TimeSpan         // Duration
Money            // Decimal with currency
Json             // JSON value
Xml              // XML document
```

### Custom Types
```cadenza
// Type aliases
type UserId = UUID
type ProductPrice = Money

// Enums
enum OrderStatus {
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

// Structured types
record User {
    id: UserId,
    name: String,
    email: Email,
    created: DateTime
}

// Validated types
type StrongPassword = String 
    where length >= 8 
    and contains_uppercase 
    and contains_lowercase 
    and contains_digit
```

## Module System

### Module Declaration
```cadenza
// File: user_service.cdz
module UserService {
    // Module-level configuration
    config {
        database_url: String from env "DATABASE_URL"
        max_connections: Int32 default 10
    }
    
    // External dependencies
    dependencies {
        logging: "Microsoft.Extensions.Logging" version "6.0.0"
        database: "EntityFramework.Core" version "6.0.0"
    }
    
    // Public interface
    public {
        function create_user(input: CreateUserInput) -> Result<User, UserError>
        function get_user(id: UserId) -> Result<User, UserError>
        type User
        type UserError
    }
    
    // Private implementation
    private {
        function validate_email(email: Email) -> Result<Unit, ValidationError>
        function hash_password(password: String) -> HashedPassword
    }
}
```

### Import System
```cadenza
// Explicit imports
import UserService.{User, create_user}
import OrderService.* // Import all public items
import PaymentService as Payment // Alias entire module

// Conditional imports
import LoggingService when debug_mode
import MetricsService when production_mode
```

### Module Composition
```cadenza
// Composite modules
module ECommerceApi {
    compose {
        UserService,
        OrderService,
        PaymentService
    }
    
    // Re-export selected items
    public {
        use UserService.{User, create_user}
        use OrderService.{Order, place_order}
        use PaymentService.{process_payment}
    }
}
```

## Functions

### Function Declaration
```cadenza
// Pure function (no side effects)
pure function calculate_tax(amount: Money, rate: Decimal) -> Money {
    return amount * rate
}

// Effectful function (has side effects)
effectful function save_user(user: User) 
    uses [Database, Logging] 
    -> Result<UserId, DatabaseError> {
    
    log_info("Saving user: ${user.name}")
    return database.save(user)
}

// Async function
async function fetch_user_data(id: UserId) 
    uses [Network, Database] 
    -> Result<User, ServiceError> {
    
    let user_data = await external_api.get_user(id)
    let user = await database.get_user(id)
    return merge_user_data(user_data, user)
}
```

### Function Overloading
```cadenza
// Cadenza uses explicit naming instead of overloading
function create_user_with_email(email: Email) -> Result<User, UserError>
function create_user_with_credentials(email: Email, password: String) -> Result<User, UserError>
function create_user_from_oauth(oauth_token: OAuthToken) -> Result<User, UserError>
```

### Higher-Order Functions
```cadenza
// Function types
type UserValidator = (User) -> Result<Unit, ValidationError>
type UserTransformer<T> = (User) -> T

// Functions as parameters
function validate_and_save_user(
    user: User,
    validator: UserValidator
) -> Result<UserId, UserError> {
    validator(user)?
    return save_user(user)
}

// Lambda expressions
let transform_users = users.map(user => user.name.to_uppercase())
```

## Effect System

### Effect Types
```cadenza
// Built-in effects
Database         // Database operations
Network          // Network calls
FileSystem       // File operations
Logging          // Logging operations
Memory           // Memory allocation
Time             // Time-dependent operations
Random           // Random number generation
Environment      // Environment variable access
```

### Effect Tracking
```cadenza
// Function signature declares all effects
function process_order(order: Order) 
    uses [Database, Network, Logging] 
    -> Result<ProcessedOrder, ProcessingError> {
    
    // All effects must be explicitly used
    log_info("Processing order ${order.id}")  // Logging effect
    let payment = await payment_service.charge(order.total)  // Network effect
    let saved_order = database.save(order)  // Database effect
    
    return ProcessedOrder.from(saved_order, payment)
}

// Effect composition
function complex_operation() 
    uses [Database, Network] 
    -> Result<ComplexResult, ComplexError> {
    
    // Can call functions with subset of effects
    let user = get_user_from_db()  // uses [Database]
    let external_data = fetch_external_data()  // uses [Network]
    
    return combine_data(user, external_data)  // pure function
}
```

### Effect Isolation
```cadenza
// Effect boundaries
isolated function unsafe_operation() 
    uses [FileSystem, Network] 
    -> Result<UnsafeResult, UnsafeError> {
    
    // Isolated effects don't propagate
    let file_data = read_file("/tmp/data.txt")
    let api_response = call_external_api(file_data)
    return process_response(api_response)
}

// Usage doesn't require declaring isolated effects
pure function safe_caller() -> Result<SafeResult, SafeError> {
    // This is allowed because unsafe_operation is isolated
    let result = unsafe_operation()
    return transform_to_safe(result)
}
```

## Error Handling

### Result Type
```cadenza
// All functions that can fail return Result
function divide(a: Float64, b: Float64) -> Result<Float64, MathError> {
    if b == 0.0 {
        return Error(MathError.DivisionByZero)
    }
    return Ok(a / b)
}

// Error propagation with ?
function complex_calculation(x: Float64, y: Float64) -> Result<Float64, MathError> {
    let intermediate = divide(x, 2.0)?  // Early return on error
    let final_result = divide(intermediate, y)?
    return Ok(final_result)
}
```

### Error Types
```cadenza
// Custom error types
enum UserError {
    NotFound(UserId),
    InvalidEmail(String),
    DuplicateEmail(Email),
    WeakPassword(String)
}

// Error with context
record DatabaseError {
    operation: String,
    table: String,
    error_code: Int32,
    message: String,
    timestamp: DateTime
}

// Error conversion
function save_user(user: User) -> Result<UserId, UserError> {
    let db_result = database.insert(user)
        .map_error(|db_err| UserError.DatabaseError(db_err))
    
    return db_result
}
```

### Panic Prevention
```cadenza
// No panic! or unwrap() - all errors must be handled
function get_user_name(id: UserId) -> String {
    let user = get_user(id)
    
    // Compile error: Result must be handled
    // return user.name  // This won't compile
    
    // Correct approaches:
    match user {
        Ok(user) => user.name,
        Error(_) => "Unknown User"
    }
    
    // Or with default
    return user.unwrap_or_default().name
}
```

## Inter-Module Communication

### Module Interfaces
```cadenza
// Interface definition
interface UserRepository {
    function get_user(id: UserId) -> Result<User, RepositoryError>
    function save_user(user: User) -> Result<UserId, RepositoryError>
    function delete_user(id: UserId) -> Result<Unit, RepositoryError>
}

// Implementation
module SqlUserRepository implements UserRepository {
    function get_user(id: UserId) -> Result<User, RepositoryError> {
        // SQL implementation
    }
    
    function save_user(user: User) -> Result<UserId, RepositoryError> {
        // SQL implementation
    }
    
    function delete_user(id: UserId) -> Result<Unit, RepositoryError> {
        // SQL implementation
    }
}
```

### Dependency Injection
```cadenza
// Service definition with dependencies
module UserService {
    dependencies {
        repository: UserRepository,
        logger: Logger,
        email_service: EmailService
    }
    
    function create_user(input: CreateUserInput) -> Result<User, UserError> {
        let user = validate_and_create_user(input)?
        let user_id = repository.save_user(user)?
        email_service.send_welcome_email(user.email)
        logger.log_info("User created: ${user_id}")
        return Ok(user)
    }
}

// Dependency wiring
module Application {
    wire {
        UserService.repository = SqlUserRepository
        UserService.logger = ConsoleLogger
        UserService.email_service = SmtpEmailService
    }
}
```

### Message Passing
```cadenza
// Event definition
record UserCreated {
    user_id: UserId,
    email: Email,
    timestamp: DateTime
}

// Event publisher
module UserService {
    function create_user(input: CreateUserInput) -> Result<User, UserError> {
        let user = create_user_internal(input)?
        
        // Publish event
        publish UserCreated {
            user_id: user.id,
            email: user.email,
            timestamp: DateTime.now()
        }
        
        return Ok(user)
    }
}

// Event subscriber
module EmailService {
    subscribe UserCreated {
        function handle_user_created(event: UserCreated) -> Result<Unit, EmailError> {
            return send_welcome_email(event.email)
        }
    }
}
```

## External Dependencies

### NuGet Integration
```cadenza
// Package specification
dependencies {
    "Newtonsoft.Json": "13.0.3",
    "Microsoft.EntityFrameworkCore": "6.0.0",
    "Serilog": "2.12.0"
}

// Automatic FFI generation
foreign import "Newtonsoft.Json" {
    namespace JsonConvert {
        function SerializeObject<T>(obj: T) -> String
            effects [Memory]
        function DeserializeObject<T>(json: String) -> T
            effects [Memory]
            throws JsonException
    }
}
```

### FFI Usage
```cadenza
// Safe wrapper around external library
function serialize_user(user: User) -> Result<String, SerializationError> {
    try {
        let json = JsonConvert.SerializeObject(user)
        return Ok(json)
    } catch JsonException as e {
        return Error(SerializationError.JsonError(e.message))
    }
}

// Automatic effect inference
function save_user_as_json(user: User) 
    uses [Memory, FileSystem] 
    -> Result<Unit, IoError> {
    
    let json = serialize_user(user)?  // Memory effect inferred
    let file_result = write_to_file("user.json", json)  // FileSystem effect
    return file_result
}
```

### External Service Integration
```cadenza
// HTTP client integration
foreign import "Microsoft.Extensions.Http" {
    interface HttpClient {
        function GetAsync(url: String) -> Task<HttpResponseMessage>
            effects [Network]
        function PostAsync(url: String, content: HttpContent) -> Task<HttpResponseMessage>
            effects [Network]
    }
}

// Service wrapper
module ExternalApiService {
    dependencies {
        http_client: HttpClient
    }
    
    function get_user_data(id: UserId) 
        uses [Network] 
        -> Result<ExternalUserData, ApiError> {
        
        let response = await http_client.GetAsync("https://api.example.com/users/${id}")
        
        if response.status_code != 200 {
            return Error(ApiError.HttpError(response.status_code))
        }
        
        let content = await response.content.ReadAsStringAsync()
        let user_data = JsonConvert.DeserializeObject<ExternalUserData>(content)
        return Ok(user_data)
    }
}
```

## API Endpoints

### REST API Definition
```cadenza
// API module
module UserApi {
    // Base configuration
    api_config {
        base_path: "/api/v1/users"
        version: "1.0"
        cors_enabled: true
    }
    
    // GET endpoint
    endpoint GET "/" {
        query_params {
            page: Int32 default 1,
            size: Int32 default 10,
            search: String optional
        }
        
        returns {
            200: PaginatedResponse<User>,
            400: ValidationError,
            500: InternalServerError
        }
        
        handler: get_users
    }
    
    // POST endpoint
    endpoint POST "/" {
        body: CreateUserRequest,
        
        returns {
            201: CreatedResponse<User>,
            400: ValidationError,
            409: ConflictError,
            500: InternalServerError
        }
        
        handler: create_user
    }
    
    // GET by ID endpoint
    endpoint GET "/{id}" {
        path_params {
            id: UserId
        }
        
        returns {
            200: User,
            404: NotFoundError,
            500: InternalServerError
        }
        
        handler: get_user_by_id
    }
}
```

### Handler Implementation
```cadenza
// Handler functions
function get_users(request: GetUsersRequest) 
    uses [Database, Logging] 
    -> Result<PaginatedResponse<User>, ApiError> {
    
    log_info("Getting users: page=${request.page}, size=${request.size}")
    
    let users = database.get_users_paginated(
        page: request.page,
        size: request.size,
        search: request.search
    )?
    
    return Ok(PaginatedResponse {
        data: users.items,
        page: users.page,
        total: users.total,
        has_next: users.has_next
    })
}

function create_user(request: CreateUserRequest) 
    uses [Database, Logging, Network] 
    -> Result<CreatedResponse<User>, ApiError> {
    
    let input = CreateUserInput {
        name: request.name,
        email: request.email,
        password: request.password
    }
    
    let user = UserService.create_user(input)
        .map_error(|err| ApiError.from_user_error(err))?
    
    return Ok(CreatedResponse {
        data: user,
        location: "/api/v1/users/${user.id}"
    })
}
```

### Middleware Support
```cadenza
// Custom middleware
module AuthenticationMiddleware {
    function authenticate(request: HttpRequest) -> Result<AuthenticatedRequest, AuthError> {
        let token = request.headers.get("Authorization")
            .ok_or(AuthError.MissingToken)?
        
        let user_id = verify_jwt_token(token)?
        
        return Ok(AuthenticatedRequest {
            original: request,
            user_id: user_id
        })
    }
}

// Apply middleware to endpoint
endpoint GET "/profile" {
    middleware: [AuthenticationMiddleware.authenticate],
    
    returns {
        200: UserProfile,
        401: UnauthorizedError,
        500: InternalServerError
    }
    
    handler: get_user_profile
}
```

## Data Structures

### Records (Immutable)
```cadenza
// Simple record
record Point {
    x: Float64,
    y: Float64
}

// Record with methods
record User {
    id: UserId,
    name: String,
    email: Email,
    created_at: DateTime
    
    // Methods
    function display_name(self) -> String {
        return self.name.to_title_case()
    }
    
    function is_recent(self) -> Bool {
        let one_week_ago = DateTime.now().subtract(TimeSpan.from_days(7))
        return self.created_at > one_week_ago
    }
}

// Record with validation
record CreateUserRequest {
    name: String where length > 0 and length <= 100,
    email: Email,
    password: String where length >= 8,
    terms_accepted: Bool where value == true
}
```

### Unions (Sum Types)
```cadenza
// Simple union
union PaymentMethod {
    CreditCard { number: String, expiry: String },
    PayPal { email: Email },
    BankTransfer { account_number: String, routing_number: String }
}

// Union with shared data
union ApiResponse<T> {
    Success { data: T, timestamp: DateTime },
    Error { error_code: String, message: String, timestamp: DateTime },
    Loading { started_at: DateTime }
}

// Pattern matching
function handle_payment(method: PaymentMethod) -> Result<PaymentResult, PaymentError> {
    match method {
        PaymentMethod.CreditCard { number, expiry } => {
            return process_credit_card(number, expiry)
        },
        PaymentMethod.PayPal { email } => {
            return process_paypal(email)
        },
        PaymentMethod.BankTransfer { account_number, routing_number } => {
            return process_bank_transfer(account_number, routing_number)
        }
    }
}
```

### Generics
```cadenza
// Generic functions
function map<T, U>(items: List<T>, transform: (T) -> U) -> List<U> {
    let result = List.empty<U>()
    for item in items {
        result.add(transform(item))
    }
    return result
}

// Generic types
record Repository<T> {
    connection: DatabaseConnection,
    
    function get_by_id(self, id: UUID) -> Result<T, RepositoryError> {
        return self.connection.query_single<T>(
            "SELECT * FROM ${T.table_name} WHERE id = ?", 
            [id]
        )
    }
}

// Generic constraints
function serialize_entity<T>(entity: T) -> Result<String, SerializationError> 
    where T: Serializable {
    return entity.to_json()
}
```

## Control Flow

### Conditional Expressions
```cadenza
// If expressions (not statements)
let status = if user.is_active() {
    "Active"
} else {
    "Inactive"
}

// Match expressions
let user_type = match user.role {
    Role.Admin => "Administrator",
    Role.User => "Regular User",
    Role.Guest => "Guest User"
}

// Guard clauses
function process_user(user: User) -> Result<ProcessedUser, ProcessingError> {
    guard user.is_valid() else {
        return Error(ProcessingError.InvalidUser)
    }
    
    guard user.is_active() else {
        return Error(ProcessingError.InactiveUser)
    }
    
    return Ok(ProcessedUser.from(user))
}
```

### Loops
```cadenza
// For loops
let squared_numbers = List.empty<Int32>()
for number in 1..10 {
    squared_numbers.add(number * number)
}

// While loops
let attempts = 0
while attempts < 3 {
    let result = try_operation()
    if result.is_ok() {
        break
    }
    attempts += 1
}

// Loop with results
let processed_items = List.empty<ProcessedItem>()
for item in items {
    let processed = process_item(item)?  // Early return on error
    processed_items.add(processed)
}
```

### Pipeline Operations
```cadenza
// Pipeline syntax
let result = input_data
    |> validate_input
    |> transform_data
    |> enrich_with_external_data
    |> save_to_database
    |> format_response

// Pipeline with error handling
let user_result = user_input
    |> validate_user_input
    |> create_user_entity
    |> save_user_to_database
    |> send_welcome_email
    |> create_user_response

// Conditional pipeline
let result = data
    |> clean_data
    |> if needs_validation { validate_data } else { identity }
    |> process_data
    |> format_output
```

This comprehensive specification provides all the fundamental building blocks for Cadenza, emphasizing safety, explicitness, and LLM-friendly design patterns.