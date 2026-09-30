using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletObjectPoolManager : MonoBehaviour
{
    [Header("オブジェクトプールの設定")]
    [SerializeField] private int _initCount = 50;
    [SerializeField] private int _maxCount = 200;
    [SerializeField] private EnumToObjectDatabase _objectDatabase;
    [SerializeField] private AudioSource _soundEffects;

    private readonly Dictionary<BulletTypeEnum, ObjectPool<PooledAttackBase>> _objectPoolDict = new();
    private void Start()
    {
        if (_objectDatabase == null)
        {
            throw new System.InvalidOperationException("BulletObjectPoolManager: 弾のデータベースが設定されていません。");
        }
        if (_soundEffects == null)
        {
            throw new System.InvalidOperationException("BulletObjectPoolManager: 効果音用 AudioSource が設定されていません。");
        }
        InitPool();
    }
    private void InitPool()
    {
        var mappings = _objectDatabase.Mappings;
        foreach (var pair in mappings)
        {
            _objectPoolDict[pair.Type] = new ObjectPool<PooledAttackBase>(
                () =>
                {
                    // pairに沿ったプレハブをインスタンス化したいのでラムダ式を用いる。
                    var bullet = Instantiate(_objectDatabase.GetGameObject(pair.Type), transform);
                    var component = bullet.GetComponent<PooledAttackBase>();
                    // プール生成時に、弾が使用するシーンの効果音出力を渡す。
                    if (component is BulletShotBehaviour shot) shot.SetAudioSource(_soundEffects);
                    component.OnInitialize();
                    component.OnReturnToPool += () =>
                    {
                        if (component.gameObject.activeSelf)
                        {
                            _objectPoolDict[pair.Type].Release(component);
                        }
                    };
                    return component;
                },
                OnGetFromPool, OnReleaseToPool, OnDisposePoolObject,
                true, _initCount, _maxCount
                );
            // プールを満たしておくため予め生成しておく
            var list = new List<PooledAttackBase>();
            for (int i = 0; i < _initCount; i++)
            {
                var component = _objectPoolDict[pair.Type].Get();
                list.Add(component);
            }
            foreach (var component in list)
            {
                _objectPoolDict[pair.Type].Release(component);
            }
        }
    }
    private void OnGetFromPool(PooledAttackBase parameter) => parameter.gameObject.SetActive(true);
    private void OnReleaseToPool(PooledAttackBase parameter) => parameter.gameObject.SetActive(false);
    private void OnDisposePoolObject(PooledAttackBase parameter) => Destroy(parameter.gameObject);

    public PooledAttackBase Get(BulletTypeEnum type) => _objectPoolDict[type].Get();
    public void Release(BulletTypeEnum type, PooledAttackBase component) => _objectPoolDict[type].Release(component);

}
