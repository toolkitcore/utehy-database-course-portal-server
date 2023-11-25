﻿using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using UTEHY.DatabaseCoursePortal.Api.Constants;
using UTEHY.DatabaseCoursePortal.Api.Data.Entities;
using UTEHY.DatabaseCoursePortal.Api.Data.EntityFrameworkCore;
using UTEHY.DatabaseCoursePortal.Api.Enums;
using UTEHY.DatabaseCoursePortal.Api.Exceptions;
using UTEHY.DatabaseCoursePortal.Api.Models.Banner;
using UTEHY.DatabaseCoursePortal.Api.Models.Common;
using UTEHY.DatabaseCoursePortal.Api.Models.Mail;
using UTEHY.DatabaseCoursePortal.Api.Models.Teacher;
using UTEHY.DatabaseCoursePortal.Api.Models.User;

namespace UTEHY.DatabaseCoursePortal.Api.Services
{
    public class TeacherService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly FileService _fileService;
        private readonly UserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly MailService _mailService;
        private readonly TwilioService _twilioService;
        private readonly IMapper _mapper;

        public TeacherService(ApplicationDbContext dbContext, FileService fileService, IMapper mapper, UserService userService, UserManager<User> userManager, MailService mailService, TwilioService twilioService)
        {
            _dbContext = dbContext;
            _fileService = fileService;
            _mapper = mapper;
            _userService = userService;
            _userManager = userManager;
            _mailService = mailService;
            _twilioService = twilioService;
        }

        public async Task<PagingResult<TeacherDto>> Get(GetTeacherRequest request)
        {
            var query = _dbContext.Teachers.Include(x => x.User).AsQueryable();

            if (!string.IsNullOrEmpty(request.NameOrEmail))
            {
                string search = request.NameOrEmail.ToLower();
                query = query.Where(b => b.User.UserName.ToLower().Contains(request.NameOrEmail.ToLower()) || b.User.Email.ToLower().Contains(request.NameOrEmail.ToLower()));
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                query = query.Where(b => b.User.PhoneNumber.ToLower().Contains(request.PhoneNumber.ToLower()));
            }

            if (request.Status != null)
            {
                query = query.Where(b => b.User.Status == request.Status);
            }

            int total = await query.CountAsync();

            if (request.PageIndex == null) request.PageIndex = 1;
            if (request.PageSize == null) request.PageSize = total;

            int totalPages = (int)Math.Ceiling((double)total / request.PageSize.Value);

            if (string.IsNullOrEmpty(request.OrderBy) && string.IsNullOrEmpty(request.SortBy))
            {
                query = query.OrderByDescending(b => b.Id);
            }
            else if (string.IsNullOrEmpty(request.OrderBy))
            {
                if(request.SortBy == SortByConstant.Asc)
                {
                    query = query.OrderBy(b => b.Id);
                }
                else
                {
                    query = query.OrderByDescending(b => b.Id);
                }
            }
            else if (string.IsNullOrEmpty(request.SortBy))
            {
                query = query.OrderByDescending(b => b.Id);
            }
            else
            {
                if(request.OrderBy == OrderByConstant.Id && request.SortBy == SortByConstant.Asc)
                {
                    query = query.OrderBy(b => b.Id);
                }
                else if(request.OrderBy == OrderByConstant.Id && request.SortBy == SortByConstant.Desc)
                {
                    query = query.OrderByDescending(b => b.Id);
                }
            }

            var items = await query
            .Skip((request.PageIndex.Value - 1) * request.PageSize.Value)
            .Take(request.PageSize.Value)
            .ToListAsync();

            var itemsMapper = _mapper.Map<List<TeacherDto>>(items);

            var result = new PagingResult<TeacherDto>(itemsMapper, request.PageIndex.Value, request.PageSize.Value, total, totalPages);

            return result;
        }

        public async Task<Teacher> Create(CreateTeacherRequest request)
        {
            if (string.IsNullOrEmpty(request.Phone) && string.IsNullOrEmpty(request.Email))
            {
                throw new ApiException("Phải nhập ít nhật 1 phương thức xác thực tài khoản!", HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(request.Email) && request.VerificationType == VerificationType.Email)
            {
                throw new ApiException("Dữ liệu trường email không được để trống khi chọn kiểu xác thực là email!", HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(request.Phone) && request.VerificationType == VerificationType.Phone)
            {
                throw new ApiException("Dữ liệu trường số điện thoại không được để trống khi chọn kiểu xác thực là số điện thoại!", HttpStatusCode.BadRequest);
            }

            var createUserRequest = _mapper.Map<CreateUserRequest>(request);
            createUserRequest.Role = Constants.Role.Teacher;

            var user = await _userService.Create(createUserRequest);

            var newTeacher = new Teacher()
            {
                UserId = user.Id,
                TeacherId = request.TeacherId,
            };

            await _dbContext.Teachers.AddAsync(newTeacher);
            await _dbContext.SaveChangesAsync();

            if(request.VerificationType == VerificationType.Email)
            {
                var otpCode = await _userManager.GenerateChangePhoneNumberTokenAsync(user, user.PhoneNumber);

                var mail = new SendMailRequest
                {
                    ToEmail = request.Email,
                    Subject = "Mã xác nhận tài khoản giáo viên UTEHY Database Course Portal",
                    Body = "Mã xác thực đăng nhập UTEHY DatabaseCourse của bạn là " + otpCode,
                };

                await _mailService.Send(mail);
            }
            else
            {
                var otpCode = await _userManager.GenerateChangePhoneNumberTokenAsync(user, user.PhoneNumber);

                string message = "Mã xác thực tài khoản giáo viên UTEHY Database Course của bạn là " + otpCode;
                await _twilioService.SendMessage(message, user.PhoneNumber);
            }

            return newTeacher;
        }

        public async Task<Banner> Edit(EditBannerRequest request)
        {
            var banner = await _dbContext.Banners.FindAsync(request.Id);

            if (banner == null)
            {
                throw new Exception("Banner không tồn tại!");
            }

            if (request.ImageFile?.Length > 0)
            {
                request.Image = await _fileService.UploadFileAsync(request.ImageFile, PathFolder.Banner);
                await _fileService.DeleteFileAsync(banner.Image);
            }
            else
            {
                request.Image = banner.Image;
            }

            _mapper.Map(request, banner);
            banner.UpdatedAt = DateTime.Now;

            await _dbContext.SaveChangesAsync();

            return banner;
        }

        public async Task<Banner> Delete(int id)
        {
            var banner = await _dbContext.Banners.FindAsync(id);

            if (banner == null)
            {
                throw new Exception("Banner không tồn tại!");
            }

            banner.DeletedAt = DateTime.Now;

            _dbContext.Banners.Remove(banner);

            await _fileService.DeleteFileAsync(banner.Image);

            await _dbContext.SaveChangesAsync();

            return banner;
        }

        public async Task<List<Banner>> DeleteMultiple(List<int> ids)
        {
            var banners = await _dbContext.Banners.Where(x => ids.Contains(x.Id)).ToListAsync();

            var invalidIds = ids.Except(banners.Select(b => b.Id)).ToList();

            if (invalidIds.Any())
            {
                throw new Exception($"Danh sách Ids banner không tồn tại: {string.Join(", ", invalidIds)}");
            }

            _dbContext.Banners.RemoveRange(banners);

            var fileUrls = banners.Select(banner => banner.Image).ToList();

            await _fileService.DeleteFilesAsync(fileUrls);

            await _dbContext.SaveChangesAsync();

            return banners;
        }
    }
}
