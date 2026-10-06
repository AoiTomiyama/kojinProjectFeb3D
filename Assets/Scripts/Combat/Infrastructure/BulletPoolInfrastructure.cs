using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletPoolInfrastructure : MonoBehaviour
{
    [Header("オブジェクトプールの設定")]
    [SerializeField] private int _initCount = 50;
    [SerializeField] private int _maxCount = 200;
    [SerializeField] private BulletPrefabCatalogConfiguration _objectDatabase;
    [SerializeField] private AudioSource _soundEffects;

    private readonly Dictionary<BulletTypeDomain, ObjectPool<PooledAttackBaseGameplay>> _objectPoolDict = new();
    private void Start()
    {
        if (_objectDatabase == null)
            throw new System.InvalidOperationException($"{name}: BulletPoolInfrastructure._objectDatabase が設定されていません。");
        if (_soundEffects == null)
            throw new System.InvalidOperationException($"{name}: BulletPoolInfrastructure._soundEffects が設定されていません。");
        InitPool();
    }
    private void InitPool()
    {
        var mappings = _objectDatabase.Mappings;
        foreach (var pair in mappings)
        {
            _objectPoolDict[pair.Type] = new ObjectPool<PooledAttackBaseGameplay>(
                () =>
                {
                    // pairに沿ったプレハブをインスタンス化したいのでラムダ式を用いる。
                    var bullet = Instantiate(_objectDatabase.GetGameObject(pair.Type), transform);
                    var component = bullet.GetComponent<PooledAttackBaseGameplay>();
                    if (component == null) throw new System.InvalidOperationException($"{bullet.name}: PooledAttackBaseGameplay が必要です。");
                    // プール生成時に、弾が使用するシーンの効果音出力を渡す。
                    if (component is BulletShotGameplay shot) shot.SetAudioSource(_soundEffects);
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
            var list = new List<PooledAttackBaseGameplay>();
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
    private void OnGetFromPool(PooledAttackBaseGameplay parameter) => parameter.gameObject.SetActive(true);
    private void OnReleaseToPool(PooledAttackBaseGameplay parameter) => parameter.gameObject.SetActive(false);
    private void OnDisposePoolObject(PooledAttackBaseGameplay parameter)
    {
        // シーン破棄後にプールがクリアされても、破棄済みの弾にはアクセスしない。
        if (parameter == null) return;
        Destroy(parameter.gameObject);
    }

    public PooledAttackBaseGameplay Get(BulletTypeDomain type) => _objectPoolDict[type].Get();
    public void Release(BulletTypeDomain type, PooledAttackBaseGameplay component) => _objectPoolDict[type].Release(component);

}
