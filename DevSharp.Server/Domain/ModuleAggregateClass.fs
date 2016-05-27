﻿namespace DevSharp.Server.Domain

open System
open System.Linq.Expressions
open FSharp.Core
open FSharp.Collections
open DevSharp.Annotations
open DevSharp.Validations
open DevSharp.Validations.ValidationUtils
open DevSharp.Domain
open DevSharp.Server

type actDelegate = Func<obj, obj, obj seq>

type ModuleAggregateClass (aggregateModule: Type) =
    
    //let aggregateName = aggregateModule.Name
    
    //let aggregateNamespace = aggregateModule.Namespace

    let aggregateFullName = aggregateModule.FullName

    let containerName = sprintf "aggregate %O" aggregateFullName

    let commandType = 
        ReflectionUtils.findModuleInnerType
            aggregateModule
            containerName
            typedefof<AggregateCommandAttribute> 
            "Command"
            false
    
    let eventType = 
        ReflectionUtils.findModuleInnerType
            aggregateModule
            containerName
            typedefof<AggregateEventAttribute> 
            "Event"
            false


    let (initialStateValue, stateType) =
        let initialStateProperty =
            ReflectionUtils.findModuleProperty
                aggregateModule
                containerName
                typedefof<AggregateInitAttribute> 
                "init"
                false
        (initialStateProperty.GetValue(null), initialStateProperty.PropertyType)

    let validateInvoker =
        let validateCommandMethod = 
            ReflectionUtils.findModuleMethod
                aggregateModule
                containerName
                typedefof<AggregateValidateAttribute> 
                "validate"
                ( ReflectionUtils.getSeqType typedefof<ValidationItem> )
                [ commandType; ]
                true

        match validateCommandMethod with
        | null ->
            (fun command -> Seq.empty)
        | _ ->
            // command => MyModule.validate ((MyModule.Command)command).Cast<object>()
            let commandParameter = Expression.Parameter(typedefof<obj>, "command")
            let castExpr = Expression.MakeUnary(ExpressionType.ConvertChecked, commandParameter, commandType)
            let callValidate = Expression.Call(validateCommandMethod, castExpr)
            let lambdaExpr = Expression.Lambda<Func<obj, ValidationItem seq>>(callValidate, commandParameter)
            let compiled = lambdaExpr.Compile()
            compiled.Invoke

    let actInvoker =
        let actMethod = 
            ReflectionUtils.findModuleMethod
                aggregateModule
                containerName
                typedefof<AggregateActAttribute> 
                "act"
                ( ReflectionUtils.getListType eventType )
                [ commandType; stateType; ]
                true

        let paramCount = actMethod.GetParameters().Length

        let commandParameter = Expression.Parameter(typedefof<obj>, "command");
        let stateParameter = Expression.Parameter(typedefof<obj>, "state");

        let castCommandExpr = Expression.MakeUnary(ExpressionType.ConvertChecked, commandParameter, commandType)
        let callAct = 
            if paramCount = 1 then
                // (command, state) => MyModule.act ((MyModule.Command)command).Cast<object>()
                Expression.Call(actMethod, castCommandExpr)
            else 
                let castStateExpr = Expression.MakeUnary(ExpressionType.ConvertChecked, stateParameter, stateType)
                if paramCount = 2 then
                    // (command, state) => MyModule.act ((MyModule.Command)command, (MyModule.State)state).Cast<object>()
                    Expression.Call(actMethod, castCommandExpr, castStateExpr)
                else
                    raise (NotSupportedException (sprintf "Aggregate function %s cannot have %i parameters" actMethod.Name paramCount))

        let callCast = Expression.Call(ReflectionUtils.getMethodEnumerableCast typedefof<obj>, callAct)
        let lambdaExpr = Expression.Lambda<actDelegate>(callCast, commandParameter, stateParameter)
        let compiled = lambdaExpr.Compile()
        compiled.Invoke


    let applyInvoker =
        let applyMethod = 
            ReflectionUtils.findModuleMethod
                aggregateModule
                containerName
                typedefof<AggregateApplyAttribute> 
                "apply"
                ( stateType )
                [ eventType; stateType; ]
                false

        match applyMethod with
        | null ->
            (fun (command, state) -> state)
        | _ ->
            let paramCount = applyMethod.GetParameters().Length

            let eventParameter = Expression.Parameter(typedefof<obj>, "event");
            let stateParameter = Expression.Parameter(typedefof<obj>, "state");

            let castEventExpr = Expression.MakeUnary(ExpressionType.ConvertChecked, eventParameter, eventType)
            let callApply = 
                if paramCount = 1 then
                    // (event, state) => MyModule.apply ((MyModule.Event)event)
                    Expression.Call(applyMethod, castEventExpr)
                else 
                    let castStateExpr = Expression.MakeUnary(ExpressionType.ConvertChecked, stateParameter, stateType)
                    if paramCount = 2 then
                        // (event, state) => MyModule.apply ((MyModule.Event), event(MyModule.State)state)
                        Expression.Call(applyMethod, castEventExpr, castStateExpr)
                    else
                        raise (NotSupportedException (sprintf "Aggregate function %s cannot have %i parameters" applyMethod.Name paramCount))

            let lambdaExpr = Expression.Lambda<Func<obj, obj, obj>>(callApply, eventParameter, stateParameter)
            let compiled = lambdaExpr.Compile()
            compiled.Invoke

    
    member this.init =
        initialStateValue

    member this.validate command =
        let items = validateInvoker command
        validationResult items

    member this.act command state =
        actInvoker(command, state)

    member this.apply event state =
        applyInvoker(event, state)


    interface IAggregateClass with
        member this.init =
            this.init

        member this.validate command =
            this.validate command

        member this.act command state =
            this.act command state

        member this.apply event state =
            this.apply event state
