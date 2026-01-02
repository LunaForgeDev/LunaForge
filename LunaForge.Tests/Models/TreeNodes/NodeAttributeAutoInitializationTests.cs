using LunaForge.Models;
using LunaForge.Models.TreeNodes;
using LunaForge.THlib.Nodes;

namespace LunaForge.Tests.Models.TreeNodes;

[TestFixture]
public class NodeAttributeAutoInitializationTests
{
    [Test]
    public void TreeNode_Constructor_ShouldAutomaticallyInitializeAttributes()
    {
        var playSound = new PlaySE();

        Assert.That(playSound.Attributes, Is.Not.Null);
        Assert.That(playSound.Attributes, Has.Count.EqualTo(4)); // Name, Volume, Pan, IgnoreDef
    }

    [Test]
    public void TreeNode_Constructor_ShouldSetAttributeDefaultValues()
    {
        var playSound = new PlaySE();

        var nameAttr = playSound.Attributes.FirstOrDefault(a => a.Name == "Name");
        Assert.That(nameAttr, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(nameAttr!.DefaultValue, Is.EqualTo("\"tan00\""));
            Assert.That(nameAttr.Value, Is.EqualTo("\"tan00\""));
        }

        var volumeAttr = playSound.Attributes.FirstOrDefault(a => a.Name == "Volume");
        Assert.That(volumeAttr, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(volumeAttr!.DefaultValue, Is.EqualTo("0.1"));
            Assert.That(volumeAttr.Value, Is.EqualTo("0.1"));
        }
    }

    [Test]
    public void TreeNode_AttributeValueChange_ShouldUpdateProperty()
    {
        var playSound = new PlaySE();
        var nameAttr = playSound.Attributes.FirstOrDefault(a => a.Name == "Name");

        nameAttr!.Value = "\"custom_sound\"";

        Assert.That(playSound.Name, Is.EqualTo("\"custom_sound\""));
    }

    [Test]
    public void TreeNode_PropertyChange_ShouldBeReflectedInAttribute()
    {
        var playSound = new PlaySE
        {
            Name = "\"new_sound\""
        };

        var nameAttr = playSound.Attributes.FirstOrDefault(a => a.Name == "Name");

        Assert.That(nameAttr, Is.Not.Null);
        Assert.That(nameAttr!.ParentNode, Is.SameAs(playSound));
    }

    [Test]
    public void TreeNode_SetAttributeValue_ShouldUpdateBothAttributeAndProperty()
    {
        var playSound = new PlaySE();

        bool success = playSound.SetAttributeValue("Volume", "0.5");

        Assert.That(success, Is.True);
        
        var volumeAttr = playSound.Attributes.FirstOrDefault(a => a.Name == "Volume");
        Assert.That(volumeAttr!.Value, Is.EqualTo("0.5"));
    }

    [Test]
    public void TreeNode_GetAttributeValue_ShouldReturnCorrectValue()
    {
        var playSound = new PlaySE();

        string panValue = playSound.GetAttributeValue("Pan");

        Assert.That(panValue, Is.EqualTo("self.x / 256"));
    }

    [Test]
    public void TreeNode_ResetToDefault_ShouldRestoreDefaultValue()
    {
        var playSound = new PlaySE();
        var nameAttr = playSound.Attributes.FirstOrDefault(a => a.Name == "Name");
        nameAttr!.Value = "\"modified\"";

        nameAttr.ResetToDefault();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(nameAttr.Value, Is.EqualTo("\"tan00\""));
            Assert.That(playSound.Name, Is.EqualTo("\"tan00\""));
        }
    }

    [Test]
    public void TreeNode_DeepCopyFrom_ShouldCopyAttributes()
    {
        var original = new PlaySE();
        original.SetAttributeValue("Name", "\"original_sound\"");
        original.SetAttributeValue("Volume", "0.8");

        var copy = new PlaySE();

        copy.DeepCopyFrom(original);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(copy.Name, Is.EqualTo("\"original_sound\""));
            Assert.That(copy.Attributes, Has.Count.EqualTo(4));
        }

        var volumeAttr = copy.Attributes.FirstOrDefault(a => a.Name == "Volume");
        Assert.That(volumeAttr!.Value, Is.EqualTo("0.8"));
    }
}
