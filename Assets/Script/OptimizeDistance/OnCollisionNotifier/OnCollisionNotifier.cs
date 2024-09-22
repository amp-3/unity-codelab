using System;
using UniRx;
using UnityEngine;

public class OnCollisionNotifierComponent : MonoBehaviour
{
    //----------------------------------------------------
    // Observer パターンによるイベント監視
    //----------------------------------------------------
    #region ===== REACTIVE_POINTER_EVENT =====
    private Subject<Collider> _onTriggerEnterSubject = new Subject<Collider>();
    public IObservable<Collider> onTriggerEnterObservable => _onTriggerEnterSubject;

    private Subject<Collider> _onTriggerStaySubject = new Subject<Collider>();
    public IObservable<Collider> onTriggerStayObservable => _onTriggerStaySubject;

    private Subject<Collider> _onTriggerExitSubject = new Subject<Collider>();
    public IObservable<Collider> onTriggerExitObservable => _onTriggerExitSubject;

    private Subject<Collision> _onCollisionEnterSubject = new Subject<Collision>();
    public IObservable<Collision> onCollisionEnterObservable => _onCollisionEnterSubject;

    private Subject<Collision> _onCollisionStaySubject = new Subject<Collision>();
    public IObservable<Collision> onCollisionStayObservable => _onCollisionStaySubject;

    private Subject<Collision> _onCollisionExitSubject = new Subject<Collision>();
    public IObservable<Collision> onCollisionExitObservable => _onCollisionExitSubject;

    #endregion //) ===== REACTIVE_POINTER_EVENT =====

    public event Action<Collider> onTriggerEnter = delegate { };
    public event Action<Collider> onTriggerStay = delegate { };
    public event Action<Collider> onTriggerExit = delegate { };

    public event Action<Collision> onCollisionEnter = delegate { };
    public event Action<Collision> onCollisionStay = delegate { };
    public event Action<Collision> onCollisionExit = delegate { };

    [SerializeField]
    private LayerMask collisionTargetLayer = new LayerMask() { value = -1 };

    private CompositeDisposable disposables = new CompositeDisposable();

    private void Awake()
    {
        _onTriggerEnterSubject.AddTo(disposables);
        _onTriggerStaySubject.AddTo(disposables);
        _onTriggerExitSubject.AddTo(disposables);
        _onCollisionEnterSubject.AddTo(disposables);
        _onCollisionStaySubject.AddTo(disposables);
        _onCollisionExitSubject.AddTo(disposables);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (CommonUtil.IsPassLayerMask(other.gameObject.layer, collisionTargetLayer))
        {
            onTriggerEnter(other);
            _onTriggerEnterSubject.OnNext(other);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (CommonUtil.IsPassLayerMask(other.gameObject.layer, collisionTargetLayer))
        {
            onTriggerExit(other);
            _onTriggerExitSubject.OnNext(other);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (CommonUtil.IsPassLayerMask(collision.gameObject.layer, collisionTargetLayer))
        {
            onCollisionEnter(collision);
            _onCollisionEnterSubject.OnNext(collision);
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (CommonUtil.IsPassLayerMask(collision.gameObject.layer, collisionTargetLayer))
        {
            onCollisionExit(collision);
            _onCollisionExitSubject.OnNext(collision);
        }
    }

    public void SetCollisionTargetLayer(LayerMask layerMask)
    {
        this.collisionTargetLayer = layerMask;
    }

    private void OnDestroy()
    {
        disposables.Dispose();
    }
}
