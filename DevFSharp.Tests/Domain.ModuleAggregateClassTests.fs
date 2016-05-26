﻿namespace DevFSharp.Domain

open System
open FSharp.Reflection
open NUnit.Framework
open DevFSharp.NUnitTests.TestHelpers
open Samples.FTodoList.TodoList

[<TestFixture>]
type ``ModuleAggregateClass tests``() = 

    let todoListModuleType = typedefof<Command>.DeclaringType
    let aggregateClass = ModuleAggregateClass(todoListModuleType) :> IAggregateClass
    let initialTitle = "TodoList title"
    let emptyState = Some {title = initialTitle; nextTaskId = 1; tasks = []}

    [<Test>] 
    member test.``loading a ModuleAggregateClass with TodoList aggregate module definition do not fail`` () =
        Assert.That(aggregateClass, Is.Not.Null)

    [<Test>] 
    member test.``validating a correct Create command should give a valid result`` () =
        let result = aggregateClass.validateCommand (Create "TodoList title")
        Assert.That(result, Is.Not.Null)
        Assert.That(result.isValid, Is.True)

    [<Test>] 
    member test.``validating an incorrect Create command should give an invalid result`` () =
        let result = aggregateClass.validateCommand (Create null)
        Assert.That(result, Is.Not.Null)
        Assert.That(result.isValid, Is.False)

    [<Test>] 
    member test.``acting on a Create command over None should return a WasCreated event`` () =
        let result = aggregateClass.processCommand None (Create "TodoList title")
        Assert.That(result, Is.EquivalentTo([ WasCreated "TodoList title" ]))

    [<Test>] 
    member test.``acting on a Create command over Some state should fail`` () =
        let astate = Some { title = ""; nextTaskId = 1; tasks = [] }
        let call = fun () -> aggregateClass.processCommand astate (Create "TodoList title") |> ignore
        Assert.That(call, Throws.TypeOf<MatchFailureException>())

    [<Test>] 
    member test.``acting on a UpdateTitle command over Some state should return a TitleWasUpdated event`` () =
        let astate = Some { title = ""; nextTaskId = 1; tasks = [] }
        let result = aggregateClass.processCommand astate (UpdateTitle "TodoList title")
        Assert.That(result, Is.EquivalentTo([ TitleWasUpdated "TodoList title" ]))

    [<Test>] 
    member test.``acting on a UpdateTitle command over None state should fail`` () =
        let call = fun () -> aggregateClass.processCommand None (UpdateTitle "TodoList title") |> ignore
        Assert.That(call, Throws.TypeOf<MatchFailureException>())

    [<Test>] 
    member test.``applying a WasCreated event over None should return Some state`` () =
        let result = aggregateClass.receiveEvent None (WasCreated "TodoList title")
        Assert.That(result, Is.EqualTo(Some {title = "TodoList title"; nextTaskId = 1; tasks = []}))

    [<Test>] 
    member test.``applying a WasCreated event over Some state should fail`` () =
        let call = fun () -> aggregateClass.receiveEvent emptyState (WasCreated "TodoList title") |> ignore
        Assert.That(call, Throws.TypeOf<MatchFailureException>())

    [<Test>] 
    member test.``applying a TitleWasUpdated event over Some state should return Some state`` () =
        let result = aggregateClass.receiveEvent emptyState (TitleWasUpdated "New TodoList title")
        Assert.That(result, Is.EqualTo(Some {title = "New TodoList title"; nextTaskId = 1; tasks = []}))

    [<Test>] 
    member test.``applying a TitleWasUpdated event over None should fail`` () =
        let call = fun () -> aggregateClass.receiveEvent None (TitleWasUpdated "New TodoList title") |> ignore
        Assert.That(call, Throws.TypeOf<MatchFailureException>())



