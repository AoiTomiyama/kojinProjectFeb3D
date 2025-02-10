using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletObjectPoolManager : MonoBehaviour
{
    [Header("オブジェクトプールの設定")]
    [SerializeField] private int _initCount = 50;
    [SerializeField] private int _maxCount = 200;
    [SerializeField] private GameObject _bulletObj;

    private IObjectPool<BulletParameter> _objectPool;
    private void Start()
    {
        _objectPool = new ObjectPool<BulletParameter>(
            OnInitializePoolObject, OnGetFromPool, OnReleaseToPool, OnDisposePoolObject,
            true, _initCount, _maxCount
            );
        var list = new List<BulletParameter>();
        for (int i = 0; i < _initCount; i++)
        {
            var component = _objectPool.Get();
            list.Add(component);
        }
        foreach (var component in list)
        {
            _objectPool.Release(component);
        }
    }
    private BulletParameter OnInitializePoolObject()
    {
        var bullet = Instantiate(_bulletObj, transform);
        var component = bullet.GetComponent<BulletParameter>();
        component.OnReturnToPool += () => _objectPool.Release(component);
        return component;
    }
    private void OnGetFromPool(BulletParameter parameter) => parameter.gameObject.SetActive(true);
    private void OnReleaseToPool(BulletParameter parameter) => parameter.gameObject.SetActive(false);
    private void OnDisposePoolObject(BulletParameter parameter) => Destroy(parameter.gameObject);
}
