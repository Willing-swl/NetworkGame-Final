using UnityEngine;
using System.Collections.Generic;


namespace NanoFrame.FSM
{
    // 状态基类接口
    public interface IState<T>
    {
        void OnEnter(T owner);
        void OnUpdate(T owner);
        void OnExit(T owner);
    }

    // 状态机管理器
    public class StateMachine<T>
    {
        private T _owner;
        private IState<T> _currentState;
        private Dictionary<System.Type, IState<T>> _states = new Dictionary<System.Type, IState<T>>();

        public StateMachine(T owner) { _owner = owner; }

        public void AddState(IState<T> state) { _states[state.GetType()] = state; }

        public void ChangeState<TState>() where TState : IState<T>
        {
            System.Type type = typeof(TState);
            if (_states.TryGetValue(type, out IState<T> newState))
            {
                _currentState?.OnExit(_owner);
                _currentState = newState;
                _currentState?.OnEnter(_owner);
            }
        }

        public void Update() { _currentState?.OnUpdate(_owner); }
    }
}