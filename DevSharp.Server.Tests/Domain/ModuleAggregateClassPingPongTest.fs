module DevSharp.Server.Domain.Tests.ModuleAggregateClassPingPongTest

open NUnit.Framework
open FsUnit
open DevSharp.Domain.Aggregates
open DevSharp.Server.Domain
open Samples.Domains.PingPong
open DevSharp.Messaging


let aggregateModuleType = typedefof<Command>.DeclaringType
let mutable aggregateClass = NopAggregateClass() :> IAggregateClass
let request = Request(new Map<string, obj>(seq []))
let message = "Helloooo!"

[<SetUp>]
let testSetup () =
    aggregateClass <- ModuleAggregateClass(aggregateModuleType)

[<Test>] 
let ``loading a ModuleAggregateClass with PingPong aggregate module definition do not fail`` () =
    aggregateClass 
    |> should not' (be Null)

[<Test>] 
let ``validating a Ping command should give a valid result`` () =
    (aggregateClass.validate Ping request).isValid
    |> should be True

[<Test>] 
let ``validating a Pong command should give a valid result`` () =
    (aggregateClass.validate Pong request).isValid
    |> should be True

[<Test>] 
let ``acting with a Ping command over some state should return a WasPinged event`` () =
    aggregateClass.act Ping null request
    |> should equal [ WasPinged ]

[<Test>] 
let ``applying a WasPonged event over any state should return the initial state`` () =
    aggregateClass.apply WasPonged null request
    |> should equal null