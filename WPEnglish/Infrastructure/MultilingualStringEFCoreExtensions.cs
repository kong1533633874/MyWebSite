using System.Linq.Expressions;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Listening.Domain.Entities;

namespace Infrastructure
{
	public static class MultilingualStringEFCoreExtensions
	{
	//	public static EntityTypeBuilder<TEntity> OwnsOneMultilingualString<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder,
	//Expression<Func<TEntity, MultilingualTitle>> navigationExpression, bool required = true, int maxLength = 200) where TEntity : class
	//	{
	//		/*
 //            * The entity type 'Episode.Name#MultilingualString' is an optional dependent using table sharing without any required non shared property 
 //            * that could be used to identify whether the entity exists. If all nullable properties contain a null value in database then an object
 //            * instance won't be created in the query. Add a required property to create instances with null values for other properties or mark the
 //            * incoming navigation as required to always create an instance.
 //            */
	//		entityTypeBuilder.OwnsOne(navigationExpression, dp =>
	//		{
	//			dp.Property(c => c.ChineseTitle).IsRequired(required).HasMaxLength(maxLength).IsUnicode();
	//			dp.Property(c => c.EnglishTitle).IsRequired(required).HasMaxLength(maxLength).IsUnicode();
	//		});
	//		entityTypeBuilder.Navigation(navigationExpression).IsRequired(required);
	//		return entityTypeBuilder;
	//	}
	}
}
